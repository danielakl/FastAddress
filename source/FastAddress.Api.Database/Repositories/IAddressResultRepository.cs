using FastAddress.Api.Database.Models;

using NetTopologySuite.Geometries;

using NodaTime;

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

    /// <summary>
    /// Find the Google place IDs of the oldest results whose <c>LastRefreshed</c> is strictly older
    /// than <paramref name="olderThan"/>, so a refresh job can re-fetch the most stale rows first.
    /// </summary>
    /// <param name="olderThan">Staleness cutoff; rows refreshed before this instant are stale.</param>
    /// <param name="limit">Maximum number of place IDs to return.</param>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    /// <returns>Up to <paramref name="limit"/> place IDs, oldest <c>LastRefreshed</c> first.</returns>
    Task<IReadOnlyList<string>> FindStalePlaceIdsAsync(Instant olderThan, int limit, CancellationToken ct = default);

    /// <summary>
    /// Hard-delete result rows by their Google place IDs. Used to drop rows whose place IDs Google
    /// no longer serves. A no-op when <paramref name="googlePlaceIds"/> is empty.
    /// </summary>
    /// <param name="googlePlaceIds">The place IDs whose rows should be removed.</param>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    Task DeleteByPlaceIdsAsync(IReadOnlyList<string> googlePlaceIds, CancellationToken ct = default);
}
