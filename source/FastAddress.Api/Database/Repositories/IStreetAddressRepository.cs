using FastAddress.Api.Database.Entities;
using FastAddress.Api.Models;

using NetTopologySuite.Geometries;

namespace FastAddress.Api.Database.Repositories;

/// <summary>
/// Repository for managing <see cref="StreetAddress"/> entities.
/// </summary>
public interface IStreetAddressRepository
{
    /// <summary>
    /// Fuzzy-search street addresses by trigram similarity, biased toward
    /// <paramref name="locationBias"/> when supplied. The similarity floor is taken from
    /// <see cref="Options.AddressSearchOptions"/>.
    /// </summary>
    /// <param name="text">The query text.</param>
    /// <param name="locationBias">Optional point used to reorder results toward nearby matches.</param>
    /// <param name="limit">Maximum number of matches to return.</param>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    /// <returns>Matches ordered by proximity-boosted relevance, each carrying its raw similarity.</returns>
    IAsyncEnumerable<StreetAddressMatch> Search(
        string text,
        Point? locationBias,
        int limit,
        CancellationToken ct = default);

    /// <summary>
    /// Idempotently upsert a street address keyed on its unique Google place ID.
    /// </summary>
    /// <param name="googlePlaceId">The unique upsert key.</param>
    /// <param name="address">The address payload to insert or update.</param>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    Task UpsertByPlaceIdAsync(string googlePlaceId, StreetAddressUpsert address, CancellationToken ct = default);
}
