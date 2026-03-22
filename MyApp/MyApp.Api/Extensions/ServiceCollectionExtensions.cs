using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp.Api.Config;
using MyApp.Api.Data;

namespace MyApp.Api.Extensions;

internal static class WebApplicationBuilderExtensions
{
    internal static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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

        var jwt = BuildAndValidateJwtSettings(builder.Configuration);
        builder.Services.AddSingleton(jwt);

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
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
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

        return builder;
    }

    private static JwtSettings BuildAndValidateJwtSettings(IConfiguration config)
    {
        var key = config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "Jwt:Key is not configured. Set it in appsettings.json or via an environment variable.");

        // Enforce a minimal key length to avoid trivially weak signing keys.
        // Adjust the threshold as needed for your security requirements.
        if (key.Length < 16)
            throw new InvalidOperationException("Jwt:Key is too short. Use a longer, high-entropy secret key.");

        var issuer = config["Jwt:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("Jwt:Issuer is not configured.");

        var audience = config["Jwt:Audience"];
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Jwt:Audience is not configured.");

        if (!int.TryParse(config["Jwt:ExpiryMinutes"], out var expiryMinutes))
            expiryMinutes = 60;

        return new JwtSettings(key, issuer, audience, expiryMinutes);
    }
}
