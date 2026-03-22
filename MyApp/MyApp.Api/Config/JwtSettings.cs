namespace MyApp.Api.Config;

internal sealed record JwtSettings(
    string Key,
    string Issuer,
    string Audience,
    int ExpiryMinutes);
