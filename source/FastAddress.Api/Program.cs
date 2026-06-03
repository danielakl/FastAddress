using FastAddress.Api.Database.Extensions;
using FastAddress.Api.Database.Options;
using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Services;
using FastAddress.Api.Services.Messages.Extensions;
using FastAddress.Api.Vendors.Extensions;
using FastAddress.Sdk.Serialization;

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

services.Configure<AddressSearchOptions>(configuration.GetSection(AddressSearchOptions.ConfigKey));
services.AddGoogleApiContract(configuration);

// Add services.
services.AddSingleton<IClock>(SystemClock.Instance);
services.AddFastAddressDbContext(configuration);
services.AddScoped<IStreetAddressRepository, StreetAddressRepository>();
services.AddScoped<IStreetAddressSearchService, StreetAddressSearchService>();
services.AddDomainEventSystem();

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
