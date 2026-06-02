using System.Runtime.CompilerServices;

using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Mapping;
using FastAddress.Api.Models;
using FastAddress.Api.Options;
using FastAddress.Api.Vendors.Contracts;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;
using FastAddress.Sdk.Helpers;

using Microsoft.Extensions.Options;

namespace FastAddress.Api.Services;

/// <inheritdoc/>
internal sealed partial class StreetAddressSearchService(
    IStreetAddressRepository addressRepo,
    IGooglePlacesService placesService,
    IOptionsMonitor<AddressSearchOptions> optionsMonitor,
    ILogger<StreetAddressSearchService> logger) : IStreetAddressSearchService
{
    /// <inheritdoc/>
    public async IAsyncEnumerable<StreetAddressSearchEntry> SearchAsync(
        SearchStreetAddressQuery query,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var hits = await ReadCacheAsync(query, ct);
        if (IsCacheHit(hits))
        {
            foreach (var hit in hits.Take(query.Limit))
            {
                yield return ToCacheEntry(hit);
            }

            yield break; // Google is never called on a cache hit.
        }

        await foreach (var result in placesService.SearchAsync(ToVendorRequest(query), ct))
        {
            if (!IsStreetAddress(result))
            {
                continue; // Drop non-address results (e.g. a city/locality) before storing or returning.
            }

            var upsert = StreetAddressFactory.From(result);
            if (upsert is not null)
            {
                // Fire write-back inline so the cache fills even if the client disconnects mid-stream.
                await PersistAsync(result.PlaceId, upsert, ct);
            }

            yield return ToGoogleEntry(result, upsert);
        }
    }

    private async Task<IReadOnlyList<StreetAddressMatch>> ReadCacheAsync(
        SearchStreetAddressQuery query, CancellationToken ct)
    {
        try
        {
            var matches = new List<StreetAddressMatch>();
            await foreach (var match in addressRepo.SearchAsync(query.Text, query.LocationBias, query.Limit, ct))
            {
                matches.Add(match);
            }

            return matches;
        }
        catch (Exception ex)
        {
            // Degrade to current behavior: a DB read failure should fall through to Google.
            LogCacheReadFailed(logger, ex, query.Text);
            return [];
        }
    }

    private async Task PersistAsync(string placeId, StreetAddressUpsert upsert, CancellationToken ct)
    {
        try
        {
            await addressRepo.UpsertByPlaceIdAsync(placeId, upsert, ct);
        }
        catch (Exception ex)
        {
            // Write-back is best-effort cache warming; never fail the request because of it.
            LogPersistFailed(logger, ex, placeId);
        }
    }

    private bool IsCacheHit(IReadOnlyList<StreetAddressMatch> hits)
    {
        if (hits.Count == 0)
        {
            return false;
        }

        // Trust the cache when the best match clears the confidence bar. A specific street address
        // realistically resolves to only one or two rows, so result count is not a useful gate.
        return hits[0].Similarity >= optionsMonitor.CurrentValue.ConfidenceThreshold;
    }

    private static bool IsStreetAddress(AddressSearchResult result) =>
        result.Types.Any(PlaceTypes.IsStreetAddressType);

    private static AddressSearchRequest ToVendorRequest(SearchStreetAddressQuery query) =>
        new() { Query = query.Text, Limit = query.Limit };

    private static StreetAddressSearchEntry ToCacheEntry(StreetAddressMatch match) =>
        new()
        {
            PlaceId = match.StreetAddress.GooglePlaceId,
            StreetLine = match.StreetAddress.StreetLine ?? string.Empty,
            PostalCode = match.StreetAddress.PostalCode,
            PostalTown = match.StreetAddress.PostalTown,
            Location = match.StreetAddress.Location!,
            Score = match.Similarity,
            IsCacheHit = true,
        };

    private static StreetAddressSearchEntry ToGoogleEntry(AddressSearchResult result, StreetAddressUpsert? upsert) =>
        new()
        {
            PlaceId = result.PlaceId,
            // Prefer the derived street line; fall back to Google's formatted address when absent.
            StreetLine = upsert?.StreetLine ?? result.ShortFormattedAddress,
            PostalCode = upsert?.PostalCode,
            PostalTown = upsert?.PostalTown,
            // Use the precision-rounded location so a miss and a later cache hit agree on coordinates.
            Location = upsert?.Location ?? SpatialHelper.MakePrecise(result.Location),
            Score = 1d / (result.OrderScore + 1),
            IsCacheHit = false,
        };

    [LoggerMessage(Level = LogLevel.Error, Message = "Cache read failed for query {Query}; falling through to Google")]
    private static partial void LogCacheReadFailed(ILogger logger, Exception exception, string query);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to persist place {PlaceId}")]
    private static partial void LogPersistFailed(ILogger logger, Exception exception, string placeId);
}
