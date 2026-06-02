using System.Text.Json;
using System.Text.Json.Serialization;

using FastAddress.Sdk.Helpers;

using NetTopologySuite.IO.Converters;

using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace FastAddress.Sdk.Serialization;

/// <summary>
/// The single source of truth for FastAddress JSON serialization. Both the API (server-side
/// serialization) and the typed <see cref="Api.IFastAddressApi"/> client configure their serializers
/// through here so requests and responses round-trip identically — camelCase naming, GeoJSON geometry,
/// and NodaTime values.
/// </summary>
public static class FastAddressJsonOptions
{
    /// <summary>
    /// Apply the FastAddress serialization settings to an existing <paramref name="options"/> instance
    /// (e.g. the options handed to <c>AddJsonOptions</c> / <c>ConfigureHttpJsonOptions</c>).
    /// </summary>
    /// <param name="options">The options to configure in place.</param>
    /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;

        options.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        options.Converters.Add(new GeoJsonConverterFactory(SpatialHelper.GeometryFactoryInstance));
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));

        return options;
    }

    /// <summary>
    /// Create a new <see cref="JsonSerializerOptions"/> instance configured with the FastAddress settings.
    /// </summary>
    /// <returns>A fresh, configured options instance.</returns>
    public static JsonSerializerOptions Create() => Configure(new JsonSerializerOptions());
}
