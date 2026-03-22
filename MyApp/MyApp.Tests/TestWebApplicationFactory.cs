using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MyApp.Tests;

/// <summary>
/// Spins up the full API in-process with an isolated SQLite database file.
/// Each factory instance gets its own DB file, deleted on disposal.
/// JWT config is left as-is from appsettings.Development.json so signing
/// and validation always use the same key.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"myapp-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); // triggers auto-migration in Program.cs

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                ["AllowedOrigins:0"] = "http://localhost:5173"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(_dbPath)}*"))
            try { File.Delete(file); } catch { /* best-effort */ }
    }
}
