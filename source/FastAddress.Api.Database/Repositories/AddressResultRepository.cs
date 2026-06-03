using System.Text;

using FastAddress.Api.Database.Models;
using FastAddress.Api.Database.Options;
using FastAddress.Sdk.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NetTopologySuite.Geometries;

using NodaTime;

namespace FastAddress.Api.Database.Repositories;

/// <inheritdoc/>
public sealed class AddressResultRepository(
    FastAddressDbContext context,
    IOptionsMonitor<AddressSearchOptions> optionsMonitor) : IAddressResultRepository
{
    // Skip re-writing a row that was refreshed this recently.
    private static readonly Duration RecentRefreshWindow = Duration.FromMinutes(5);

    /// <inheritdoc/>
    public IAsyncEnumerable<StreetAddressMatch> Search(
        string text,
        Point? locationBias,
        int limit)
    {
        var normalized = text.NormalizeSingleLine(toUpperCase: true);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return AsyncEnumerable.Empty<StreetAddressMatch>();
        }

        var options = optionsMonitor.CurrentValue;
        var minSimilarity = options.MinSimilarity;

        // Two filter gates: GIN trigram filter for fuzzy matches, and an ILike prefix match
        // so exact-start hits surface before they reach the similarity threshold.
        var prefixPattern = new StringBuilder(normalized)
                .Replace("%", string.Empty)
                .Replace("_", string.Empty)
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Replace("^", string.Empty)
                .Append('%').ToString();

        return context.StreetAddressResults
            .AsNoTracking()
            .Select(sa => new Candidate
            {
                Entity = sa,
                TextScore = EF.Functions.TrigramsSimilarity(sa.SearchText!, normalized),
                PrefixScore = normalized.Length > sa.SearchText!.Length ? 0 : (double)normalized.Length / sa.SearchText!.Length,
                IsPrefixMatch = EF.Functions.ILike(sa.SearchText!, prefixPattern),
                DistanceScore = locationBias == null || sa.Location == null ? 0 :  sa.Location.Distance(locationBias) / options.BiasScaleMeters,
            })
            .Where(c => c.TextScore >= minSimilarity || c.IsPrefixMatch)
            .OrderByDescending(c => c.TextScore)
            .ThenByDescending(c => c.PrefixScore)
            .ThenByDescending(c => c.Entity.Id)
            .Take(limit)
            .Select(c => new StreetAddressMatch { StreetAddress = c.Entity, Similarity = c.TextScore })
            .AsAsyncEnumerable();
    }

    /// <inheritdoc/>
    public async Task UpsertRangeAsync(IReadOnlyList<StreetAddressUpsert> addresses, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(addresses);

        // Drop null entries and collapse duplicate place IDs within the batch.
        var deduped = addresses
            .OfType<StreetAddressUpsert>()
            .DistinctBy(a => a.GooglePlaceId)
            .ToList();

        if (deduped.Count == 0)
        {
            return;
        }

        var placeIds = deduped.Select(a => a.GooglePlaceId).ToList();
        var byPlaceId = await context.StreetAddressResults
            .Where(a => placeIds.Contains(a.GooglePlaceId))
            .ToDictionaryAsync(a => a.GooglePlaceId, ct);

        var now = context.Clock.GetCurrentInstant();
        var recentCutoff = now - RecentRefreshWindow;

        foreach (var address in deduped)
        {
            if (byPlaceId.TryGetValue(address.GooglePlaceId, out var entity))
            {
                // Leave recently-refreshed rows untouched rather than bumping them again.
                if (entity.LastRefreshed >= recentCutoff)
                {
                    continue;
                }
            }
            else
            {
                entity = new Entities.StreetAddressResult { GooglePlaceId = address.GooglePlaceId };
                context.StreetAddressResults.Add(entity);
            }

            entity.StreetLine = address.StreetLine;
            entity.PostalCode = address.PostalCode;
            entity.PostalTown = address.PostalTown;
            entity.Country = address.Country;
            entity.SearchText = address.SearchText;
            entity.Location = address.Location;
            entity.LastRefreshed = now;
        }

        await context.SaveChangesAsync(ct);
    }

    /// <summary>Intermediate projection: the materialized candidate plus its raw scoring inputs.</summary>
    private sealed class Candidate
    {
        public required Entities.StreetAddressResult Entity { get; init; }
        public required double TextScore { get; init; }
        public required double PrefixScore { get; init; }
        public required bool IsPrefixMatch { get; init; }
        public required double DistanceScore { get; init; }
    }
}
