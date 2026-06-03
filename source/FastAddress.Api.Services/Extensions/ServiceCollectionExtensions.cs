using FastAddress.Api.Services.BackgroundServices;
using FastAddress.Api.Services.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FastAddress.Api.Services.Extensions;

/// <summary>
/// DI registration for the address-refresh background job.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the hosted background service that periodically refreshes stale results, binding its
    /// <see cref="AddressRefreshOptions"/> from configuration. The <see cref="IAddressRefreshService"/> it
    /// resolves is registered by the host.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration source for <see cref="AddressRefreshOptions"/>.</param>
    public static IServiceCollection AddAddressRefresh(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AddressRefreshOptions>(configuration.GetSection(AddressRefreshOptions.ConfigKey));
        services.AddHostedService<AddressRefreshBackgroundService>();
        return services;
    }
}
