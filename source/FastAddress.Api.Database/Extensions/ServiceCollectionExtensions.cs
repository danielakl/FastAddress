using FastAddress.Api.Database.Options;
using FastAddress.Sdk.Helpers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FastAddress.Api.Database.Extensions;

/// <summary>
/// DI registration for the FastAddress database context.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Bind <see cref="DatabaseOptions"/> from configuration and register the
    /// <see cref="FastAddressDbContext"/> against PostgreSQL (PostGIS + NodaTime).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration source for <see cref="DatabaseOptions"/>.</param>
    public static IServiceCollection AddFastAddressDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.ConfigKey));

        services.AddDbContext<FastAddressDbContext>((sp, opts) =>
        {
            var databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            opts.EnableDetailedErrors(databaseOptions.EnableDetailedErrors)
                .EnableSensitiveDataLogging(databaseOptions.EnableSensitiveDataLogging)
                .UseNpgsql(databaseOptions.ConnectionString,
                    npgsqlOpts =>
                    {
                        npgsqlOpts.MigrationsHistoryTable("_migration_history", FastAddressDbContext.SchemaName);
                        npgsqlOpts.UseNetTopologySuite(
                            SpatialHelper.GeometryFactoryInstance.CoordinateSequenceFactory,
                            SpatialHelper.PrecisionModelInstance
                        );
                        npgsqlOpts.UseNodaTime();
                        npgsqlOpts.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }
            );
        });

        return services;
    }
}
