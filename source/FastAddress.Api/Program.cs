using FastAddress.Api.Database.Extensions;
using FastAddress.Api.Database.Options;
using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Infrastructure;
using FastAddress.Api.Services;
using FastAddress.Api.Services.Messages.Extensions;
using FastAddress.Api.Vendors.Extensions;
using FastAddress.Sdk.Logging;
using FastAddress.Sdk.Serialization;

using NodaTime;

using Serilog;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Route all logging through Serilog - always console and Seq if configured
builder.Host.UseFastAddressSerilog();

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

services.Configure<AddressSearchOptions>(configuration.GetSection(AddressSearchOptions.ConfigKey));
services.Configure<SeqLoggingOptions>(configuration.GetSection(SeqLoggingOptions.ConfigKey));

// Add services.
services.AddSingleton<IClock>(SystemClock.Instance);

services.AddScoped<IAddressQueryRepository, AddressQueryRepository>();
services.AddScoped<IAddressResultRepository, AddressResultRepository>();
services.AddScoped<IAddressSearchService, AddressSearchService>();

services.AddFastAddressDbContext(configuration);
services.AddGoogleApiContract(configuration);
services.AddDomainEventSystem();

services.AddProblemDetails();
services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

services.AddHealthChecks();
services.AddControllers(opts => opts.Filters.Add<SerilogEnricherMvcFilter>())
    .AddControllersAsServices()
    .AddJsonOptions(opts => FastAddressJsonOptions.Configure(opts.JsonSerializerOptions));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
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
