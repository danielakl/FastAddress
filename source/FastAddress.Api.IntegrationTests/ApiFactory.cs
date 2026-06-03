using FastAddress.Api.Database;
using FastAddress.Api.Database.Extensions;
using FastAddress.Api.Database.Options;
using FastAddress.Api.Vendors.Contracts;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using Testcontainers.PostgreSql;

namespace FastAddress.Api.IntegrationTests;

/// <summary>
/// Runs the API against a throwaway PostGIS server (its own container on a random port, so it never
/// touches a local database) with the real Npgsql trigram and spatial queries. The Google Places
/// boundary is substituted so no external calls are made, and background hosted services are removed
/// so each test's request/response behavior stays deterministic.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Same image as docker-compose so PostGIS and pg_trgm are available for the migrations.
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgis/postgis:18-3.6-alpine")
        .Build();

    /// <summary>The substituted Google Places service. Configure it per test, then clear its calls.</summary>
    public IGooglePlacesService GooglePlacesMock { get; } = Substitute.For<IGooglePlacesService>();

    /// <summary>Run an action against a fresh database scope, for seeding or assertions.</summary>
    public async Task WithDbContextAsync(Func<FastAddressDbContext, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FastAddressDbContext>();
        await action(context);
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _database.StartAsync();

        // Apply every migration to the fresh container so the schema matches production exactly.
        await WithDbContextAsync(context => context.Database.MigrateAsync());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // The Google key is never used (the service is substituted) but is required configuration.
        builder.UseSetting("Google:ApiKey", "integration-test");

        builder.ConfigureTestServices(services =>
        {
            // No background jobs during tests, so persistence and refresh stay off the request path.
            services.RemoveAll<IHostedService>();

            // Substitute the external Google Places dependency.
            services.RemoveAll<IGooglePlacesService>();
            services.AddScoped(_ => GooglePlacesMock);

            // Re-point the context at the container, reusing the production registration verbatim.
            services.RemoveAll<DbContextOptions<FastAddressDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<FastAddressDbContext>();

            var databaseConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{DatabaseOptions.ConfigKey}:{nameof(DatabaseOptions.ConnectionString)}"] = _database.GetConnectionString(),
                })
                .Build();

            services.AddFastAddressDbContext(databaseConfiguration);
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}
