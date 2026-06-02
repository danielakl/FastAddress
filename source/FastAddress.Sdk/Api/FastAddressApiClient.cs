using FastAddress.Sdk.Serialization;

using Microsoft.Extensions.DependencyInjection;

using Refit;

namespace FastAddress.Sdk.Api;

/// <summary>
/// Helpers for configuring the <see cref="IFastAddressApi"/> Refit client.
/// </summary>
public static class FastAddressApiClient
{
    /// <summary>
    /// Build the <see cref="RefitSettings"/> for the FastAddress API, using the shared
    /// <see cref="FastAddressJsonOptions"/> so the client serializes exactly as the API does.
    /// </summary>
    /// <returns>Configured Refit settings.</returns>
    public static RefitSettings CreateRefitSettings() =>
        new() { ContentSerializer = new SystemTextJsonContentSerializer(FastAddressJsonOptions.Create()) };

    /// <summary>
    /// Register <see cref="IFastAddressApi"/> as a Refit client pointed at <paramref name="baseAddress"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseAddress">The base address of the FastAddress API.</param>
    /// <returns>The Refit client builder for further configuration.</returns>
    public static IHttpClientBuilder AddFastAddressApiClient(this IServiceCollection services, Uri baseAddress)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(baseAddress);

        return services
            .AddRefitClient<IFastAddressApi>(CreateRefitSettings())
            .ConfigureHttpClient(client => client.BaseAddress = baseAddress);
    }
}
