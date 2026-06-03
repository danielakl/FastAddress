using FastAddress.Api.Vendors.Models;

namespace FastAddress.Api.Vendors.Contracts;

/// <summary>Google Places API service.</summary>
public interface IGooglePlacesService
{
    /// <summary>
    /// Search for places using Google Places API. Results are streamed as the underlying Place Details lookups complete.
    /// </summary>
    /// <param name="request">Requested search parameters.</param>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    /// <returns>Address search results.</returns>
    IAsyncEnumerable<GooglePlace> SearchPlaces(GooglePlacesSearchRequest request, CancellationToken ct = default);

    /// <summary>
    /// Fetch a single place by its ID via Place Details. When the place has relocated, the returned
    /// <see cref="GooglePlace.PlaceId"/> is the replacement ID (one hop) rather than the requested one.
    /// </summary>
    /// <param name="placeId">The Google place identifier to fetch.</param>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    /// <returns>
    /// The place, or <see langword="null"/> when Google reports the ID as obsolete (NOT_FOUND) or
    /// returns no usable address. Throws <see cref="Sdk.Exceptions.ProblemDetailsException"/> for other
    /// failures (for example an invalid ID or an exhausted quota).
    /// </returns>
    Task<GooglePlace?> FetchPlaceAsync(string placeId, CancellationToken ct = default);
}
