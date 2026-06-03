using FastAddress.Api.Database.Models;

using NetTopologySuite.Geometries;

namespace FastAddress.Api.Database.Repositories;

/// <summary>
/// Repository for managing <see cref="StreetAddress"/> entities.
/// </summary>
public interface IAddressResultRepository
{
    /// <summary>
    /// Fuzzy-search street addresses by trigram similarity, biased toward
    /// <paramref name="locationBias"/> when supplied. The similarity floor is taken from
    /// <see cref="Options.AddressSearchOptions"/>.
    /// </summary>
    /// <param name="text">The query text.</param>
    /// <param name="locationBias">Optional point used to reorder results toward nearby matches.</param>
    /// <param name="limit">Maximum number of matches to return.</param>
    /// <returns>Matches ordered by proximity-boosted relevance, each carrying its raw similarity.</returns>
    IAsyncEnumerable<StreetAddressMatch> Search(
        string text,
        Point? locationBias,
        int limit);

    /// <summary>
    /// Idempotently upsert a batch of street addresses, each keyed on its unique
    /// <see cref="StreetAddressUpsert.GooglePlaceId"/>, in a single round-trip.
    /// </summary>
    /// <param name="addresses">The address payloads to insert or update.</param>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    Task UpsertRangeAsync(IReadOnlyList<StreetAddressUpsert> addresses, CancellationToken ct = default);
}
