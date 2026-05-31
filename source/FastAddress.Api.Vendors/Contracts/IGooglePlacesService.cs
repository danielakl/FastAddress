using FastAddress.Api.Vendors.Models;

namespace FastAddress.Api.Vendors.Contracts;

/// <summary>Google Places API service.</summary>
public interface IGooglePlacesService
{
    /// <summary>
    /// Search for places using Google Places API. Results are streamed
    /// as the underlying Place Details lookups complete. Use <see cref="AddressSearchResult.OrderScore"/>
    /// to recover the correct ranking.
    /// </summary>
    /// <param name="request">Requested search parameters.</param>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    /// <returns>Address search results.</returns>
    IAsyncEnumerable<AddressSearchResult> SearchAsync(AddressSearchRequest request, CancellationToken ct = default);
}
