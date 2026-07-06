using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Tankbuch.IntegrationTests;

/// <summary>
/// Startet ein echtes PostgreSQL per Testcontainers und hostet die API in-memory
/// (WebApplicationFactory). Kein Ollama nötig → Vision fällt auf simulierte Werte zurück.
/// </summary>
public sealed class TankbuchApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:tankbuchdb", _postgres.GetConnectionString());
        builder.UseSetting("SeedDemoData", "false"); // Tests kontrollieren ihre Daten selbst
        builder.UseEnvironment("Testing");
    }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<TankbuchApiFactory>;
