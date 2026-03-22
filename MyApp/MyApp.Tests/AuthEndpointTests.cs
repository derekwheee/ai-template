using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MyApp.Tests;

public sealed class RegisterTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RegisterTests(TestWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Register_ValidCredentials_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { username = "alice", password = "password123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.NotNull(body?.Message);
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns400()
    {
        var first = await _client.PostAsJsonAsync("/api/auth/register",
            new { username = "bob", password = "password123" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { username = "bob", password = "different-password" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordTooShort_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { username = "charlie", password = "abc" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class LoginTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginTests(TestWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndUsername()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new { username = "diana", password = "password123" });

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "diana", password = "password123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrEmpty(body?.Token));
        Assert.Equal("diana", body?.Username);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new { username = "eve", password = "password123" });

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "eve", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "nobody", password = "password123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public sealed class MeTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public MeTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Me_WithValidToken_ReturnsIdAndUsername()
    {
        // Use a single client so the token is signed and validated by the same server instance
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new { username = "frank", password = "password123" });
        var loginRes = await client.PostAsJsonAsync("/api/auth/login",
            new { username = "frank", password = "password123" });
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var login = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login?.Token);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.Token);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.False(string.IsNullOrEmpty(body?.Id));
        Assert.Equal("frank", body?.Username);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithInvalidToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not.a.valid.token");

        var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public sealed class StartupValidationTests
{
    [Fact]
    public void Startup_EmptyJwtKey_ThrowsOnStartup()
    {
        // "Testing" environment loads only appsettings.json (Jwt:Key = ""),
        // not appsettings.Development.json, so the guard in Program.cs fires.
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

        var ex = Record.Exception(() => factory.CreateClient());
        Assert.NotNull(ex);
        Assert.Contains("Jwt:Key", ex.ToString());
    }
}

// ── Response DTOs ────────────────────────────────────────────────────────────

file record MessageResponse(string Message);
file record LoginResponse(string Token, string Username);
file record MeResponse(string Id, string Username);
