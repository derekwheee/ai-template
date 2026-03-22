using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MyApp.Api.Config;
using MyApp.Api.Data;

namespace MyApp.Api.Routes;

internal static class AuthRoutes
{
    internal static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", HandleRegister);
        group.MapPost("/login", HandleLogin);
        group.MapGet("/me", HandleMe).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> HandleRegister(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager)
    {
        var user = new ApplicationUser { UserName = request.Username, Email = request.Username };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Results.Ok(new { message = "User registered successfully" });
    }

    private static async Task<IResult> HandleLogin(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        JwtSettings jwt)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return Results.Unauthorized();

        var token = GenerateJwtToken(user, jwt);
        return Results.Ok(new { token, username = user.UserName });
    }

    private static IResult HandleMe(ClaimsPrincipal principal) =>
        Results.Ok(new
        {
            id = principal.FindFirstValue(ClaimTypes.NameIdentifier),
            username = principal.FindFirstValue(ClaimTypes.Name)
        });

    private static string GenerateJwtToken(ApplicationUser user, JwtSettings jwt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

internal record RegisterRequest(string Username, string Password);
internal record LoginRequest(string Username, string Password);
