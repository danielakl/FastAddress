using FastAddress.Api.Database;
using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Events.Extensions;
using FastAddress.Api.Options;
using FastAddress.Api.Services;
using FastAddress.Api.Vendors.Extensions;
using FastAddress.Sdk.Helpers;
using FastAddress.Sdk.Serialization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NodaTime;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Setup host.
builder.WebHost.UseKestrel(opts => opts.AddServerHeader = false);
builder.WebHost.UseDefaultServiceProvider((_, opts) =>
{
    opts.ValidateOnBuild = true;
    opts.ValidateScopes = true;
});

// Setup configuration.
var configuration = builder.Configuration
    .AddUserSecrets<Program>()
    .Build();

services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.ConfigKey));
services.Configure<AddressSearchOptions>(configuration.GetSection(AddressSearchOptions.ConfigKey));
services.AddGoogleApiContract(configuration);

// Add services.
services.AddSingleton<IClock>(SystemClock.Instance);
services.AddScoped<IStreetAddressRepository, StreetAddressRepository>();
services.AddScoped<IStreetAddressSearchService, StreetAddressSearchService>();

services.AddDomainEventSystem();

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

services.AddHealthChecks();
services.AddControllers()
    .AddControllersAsServices()
    .AddJsonOptions(opts => FastAddressJsonOptions.Configure(opts.JsonSerializerOptions));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();
app.MapHealthChecks("/health");
app.MapControllers();

await app.RunAsync();

namespace FastAddress.Api
{
    /// <summary>
    /// Program main entrypoint.
    /// </summary>
    /// <remarks>Needed for test projects.</remarks>
    public partial class Program { }
}
