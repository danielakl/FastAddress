using FastAddress.Api.Services.Models;

namespace FastAddress.Api.Services;

/// <summary>
/// Street address search service.
/// </summary>
public interface IAddressSearchService
{
    /// <summary>
    /// Search for street addresses.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    /// <returns>Matching street address entries.</returns>
    IAsyncEnumerable<StreetAddressSearchEntry> Search(SearchStreetAddressQuery query, CancellationToken ct = default);
}
