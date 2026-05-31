using FastAddress.Api.Vendors.Contracts;
using FastAddress.Api.Vendors.Google.DelegatingHandlers;
using FastAddress.Api.Vendors.Google.Places;
using FastAddress.Api.Vendors.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Refit;

namespace FastAddress.Api.Vendors.Extensions;

/// <summary>
/// DI registration methods for setting up external contracts.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static readonly Uri PlacesBaseAddress = new("https://places.googleapis.com");

    /// <summary>
    /// Register the services to facilitate Google API integration.
    /// </summary>
    public static IServiceCollection AddGoogleApiContract(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GoogleApisOptions>(configuration.GetSection(GoogleApisOptions.ConfigKey));

        services.AddTransient<GoogleApiKeyHandler>();

        services.AddRefitClient<IPlacesApi>()
            .ConfigureHttpClient(c => c.BaseAddress = PlacesBaseAddress)
            .AddHttpMessageHandler<GoogleApiKeyHandler>();

        services.AddScoped<IGooglePlacesService, GooglePlacesService>();

        return services;
    }
}
