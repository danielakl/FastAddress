using FastAddress.Api.Database.Models;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;
using FastAddress.Sdk.Extensions;
using FastAddress.Sdk.Helpers;

namespace FastAddress.Api.Services.Mapping;

/// <summary>
/// Builds a persistable <see cref="StreetAddressUpsert"/> from a Google <see cref="AddressSearchResult"/>.
/// </summary>
internal static class ModelMapper
{
    /// <summary>
    /// Project a Google result onto an upsert payload, returning <see langword="null"/> when no street
    /// line can be derived.
    /// </summary>
    /// <param name="result">The Google search result.</param>
    /// <returns>The upsert payload, or <see langword="null"/> when the result has no street line.</returns>
    public static StreetAddressUpsert? From(AddressSearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        // Prefer a precise route + number. Fallback to a named premise (e.g. an airport).
        var streetLine = JoinNonEmpty(
            GetComponent(AddressComponentTypes.Route, result),
            GetComponent(AddressComponentTypes.StreetNumber, result))
            ?? GetComponent(AddressComponentTypes.Premise, result);

        if (string.IsNullOrWhiteSpace(streetLine))
        {
            return null;
        }

        return new StreetAddressUpsert
        {
            GooglePlaceId = result.PlaceId,
            StreetLine = streetLine,
            PostalCode = GetComponent(AddressComponentTypes.PostalCode, result),
            PostalTown = GetComponent(AddressComponentTypes.PostalTown, result)
                ?? GetComponent(AddressComponentTypes.Locality, result),
            Country = GetComponent(AddressComponentTypes.Country, result),
            // Same normalization as query-time trigram comparisons.
            SearchText = streetLine.NormalizeSingleLine(toUpperCase: true),
            // Apply precision before storage.
            Location = SpatialHelper.MakePrecise(result.Location),
        };
    }

    private static string? GetComponent(string componentType, AddressSearchResult fromResult)
    {
        var component = fromResult.AddressComponents.FirstOrDefault(c => c.Types.Contains(componentType, StringComparer.OrdinalIgnoreCase));
        if (component is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(component.LongText) ? component.ShortText : component.LongText;
    }

    private static string? JoinNonEmpty(params string?[] parts)
    {
        var joined = string.Join(' ', parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(joined) ? null : joined;
    }
}
