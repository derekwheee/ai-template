using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// JWT auth — capture all values at startup so signing and validation always use the same key
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("Jwt:Key is not configured. Set it in appsettings.json or via an environment variable.");
var jwtIssuer    = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience  = builder.Configuration["Jwt:Audience"]!;
var jwtExpiryMin = int.Parse(builder.Configuration["Jwt:ExpiryMinutes"] ?? "60");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Apply migrations automatically in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Auth endpoints
var auth = app.MapGroup("/api/auth");

auth.MapPost("/register", async (
    RegisterRequest request,
    UserManager<ApplicationUser> userManager) =>
{
    var user = new ApplicationUser { UserName = request.Username, Email = request.Username };
    var result = await userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
        return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });

    return Results.Ok(new { message = "User registered successfully" });
});

auth.MapPost("/login", async (
    LoginRequest request,
    UserManager<ApplicationUser> userManager) =>
{
    var user = await userManager.FindByNameAsync(request.Username);
    if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        return Results.Unauthorized();

    var token = GenerateJwtToken(user, jwtKey, jwtIssuer, jwtAudience, jwtExpiryMin);
    return Results.Ok(new { token, username = user.UserName });
});

auth.MapGet("/me", (ClaimsPrincipal principal) => Results.Ok(new
{
    id = principal.FindFirstValue(ClaimTypes.NameIdentifier),
    username = principal.FindFirstValue(ClaimTypes.Name)
})).RequireAuthorization();

app.Run();

static string GenerateJwtToken(ApplicationUser user, string jwtKey, string jwtIssuer, string jwtAudience, int jwtExpiryMin)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.UserName!),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(jwtExpiryMin),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

record RegisterRequest(string Username, string Password);
record LoginRequest(string Username, string Password);

public partial class Program { }
