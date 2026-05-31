using System.Text.Json;
using System.Text.Json.Serialization;

using FastAddress.Api.Database;
using FastAddress.Api.Options;
using FastAddress.Api.Vendors.Extensions;
using FastAddress.Sdk.Helpers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NetTopologySuite.IO.Converters;

using NodaTime;
using NodaTime.Serialization.SystemTextJson;

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
services.AddGoogleApiContract(configuration);

// Add services.
services.AddSingleton<IClock>(SystemClock.Instance);

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
    .AddJsonOptions(opts =>
    {
        var serializerOpts = opts.JsonSerializerOptions;
        serializerOpts.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        serializerOpts.Converters.Add(new GeoJsonConverterFactory(SpatialHelper.GeometryFactoryInstance));
        serializerOpts.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
        serializerOpts.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        serializerOpts.PropertyNameCaseInsensitive = true;
        serializerOpts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

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
