using FastAddress.Api.Vendors.Google.DelegatingHandlers;
using FastAddress.Api.Vendors.Google.Places.Models;

using Refit;

namespace FastAddress.Api.Vendors.Google.Places;

/// <summary>
/// Refit binding for Google's Places API (new) at <c>https://places.googleapis.com</c>.
/// Authentication and field-mask headers are added by <see cref="GoogleApiKeyHandler"/>.
/// </summary>
[Headers("Accept: application/json")]
public interface IPlacesApi
{
    /// <summary>Autocomplete predictions for a partial address input.</summary>
    [Post("/v1/places:autocomplete")]
    Task<AutocompleteResponse> AutocompleteAsync(
        [Body] AutocompleteRequest body,
        [Header("X-Goog-FieldMask")] string fields,
        string languageCode,
        string regionCode,
        CancellationToken ct = default);

    /// <summary>Fetch details for a single place. <paramref name="fields"/> selects the response field mask.</summary>
    [Get("/v1/places/{placeId}")]
    Task<PlaceDetailsResponse> GetPlaceAsync(
        string placeId,
        string fields,
        string languageCode,
        string regionCode,
        CancellationToken ct = default);
}
