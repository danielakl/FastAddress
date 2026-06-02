using System.Runtime.CompilerServices;
using System.Text;

using FastAddress.Api.Models;
using FastAddress.Api.Options;
using FastAddress.Sdk.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NetTopologySuite.Geometries;

using NodaTime;

namespace FastAddress.Api.Database.Repositories;

/// <inheritdoc/>
internal sealed class StreetAddressRepository(
    FastAddressDbContext context,
    IOptionsMonitor<AddressSearchOptions> optionsMonitor) : IStreetAddressRepository
{
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
        var cutoff = options.MaxReuseAge is { } maxAge
            ? context.Clock.GetCurrentInstant() - maxAge
            : (Instant?)null;

        // Two filter gates: GIN trigram filter for fuzzy matches, and an ILike prefix match
        // so exact-start hits surface before they reach the similarity threshold.
        var prefixPattern = new StringBuilder(normalized)
                .Replace("%", string.Empty)
                .Replace("_", string.Empty)
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Replace("^", string.Empty)
                .Append('%').ToString();

        return context.StreetAddresses
            .AsNoTracking()
            .Where(sa => cutoff == null || sa.LastRefreshed >= cutoff)
            .Select(sa => new Candidate
            {
                Entity = sa,
                TextScore = EF.Functions.TrigramsSimilarity(sa.SearchText!, normalized),
                PrefixScore = (double)normalized.Length / sa.SearchText!.Length,
                IsPrefixMatch = EF.Functions.ILike(sa.SearchText!, prefixPattern),
                DistanceScore = locationBias == null || sa.Location == null ? 0 :  sa.Location.Distance(locationBias) / options.BiasScaleMeters,
            })
            .Where(c => c.TextScore >= minSimilarity || c.IsPrefixMatch)
            .OrderByDescending(c => Math.Max(c.TextScore, c.PrefixScore))
            .ThenByDescending(c => c.DistanceScore)
            .ThenByDescending(c => c.Entity.Id)
            .Take(limit)
            .Select(c => new StreetAddressMatch { StreetAddress = c.Entity, Similarity = c.TextScore })
            .AsAsyncEnumerable();
    }

    /// <inheritdoc/>
    public async Task UpsertByPlaceIdAsync(string googlePlaceId, StreetAddressUpsert address, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(googlePlaceId);
        ArgumentNullException.ThrowIfNull(address);

        var existing = await context.StreetAddresses
            .SingleOrDefaultAsync(a => a.GooglePlaceId == googlePlaceId, ct);

        var isNew = existing is null;
        existing ??= new Entities.StreetAddress { GooglePlaceId = googlePlaceId };

        existing.StreetLine = address.StreetLine;
        existing.PostalCode = address.PostalCode;
        existing.PostalTown = address.PostalTown;
        existing.Country = address.Country;
        existing.SearchText = address.SearchText;
        existing.Location = address.Location;
        existing.LastRefreshed = context.Clock.GetCurrentInstant();

        if (isNew)
        {
            context.StreetAddresses.Add(existing);
        }

        await context.SaveChangesAsync(ct);
    }

    /// <summary>Intermediate projection: the materialized candidate plus its raw scoring inputs.</summary>
    private sealed class Candidate
    {
        public required Entities.StreetAddress Entity { get; init; }
        public required double TextScore { get; init; }
        public required double PrefixScore { get; init; }
        public required bool IsPrefixMatch { get; init; }
        public required double DistanceScore { get; init; }
    }
}
