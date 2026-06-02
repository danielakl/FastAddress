using System.Runtime.CompilerServices;

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
    /// <summary>How many candidates to pull per requested result before the in-memory proximity re-rank.</summary>
    private const int CandidateMultiplier = 4;

    /// <summary>Shortest normalized query that activates prefix (ILike) matching; below this, single
    /// characters would match nearly everything.</summary>
    private const int MinPrefixMatchLength = 3;

    /// <summary>Effective similarity granted to a prefix match so an exact-start hit is trusted as a
    /// confident cache hit even when its raw trigram score is low. Comfortably exceeds the default
    /// confidence threshold (0.50).</summary>
    private const double PrefixMatchSimilarity = 0.95;

    /// <inheritdoc/>
    public async IAsyncEnumerable<StreetAddressMatch> Search(
        string text,
        Point? locationBias,
        int limit,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var normalized = text.NormalizeSingleLine(toUpperCase: true);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            yield break;
        }

        var options = optionsMonitor.CurrentValue;
        var minSimilarity = options.MinSimilarity;
        var cutoff = options.MaxReuseAge is { } maxAge
            ? context.Clock.GetCurrentInstant() - maxAge
            : (Instant?)null;

        // Two candidate gates: the GIN trigram filter for fuzzy matches, plus an ILike prefix match
        // (for queries long enough to be selective) so exact-start hits surface before they reach the
        // similarity threshold. Pull an extra candidate buffer, then re-rank by the proximity
        // composite in memory so we stay robust against Npgsql translation limits on the decay arithmetic.
        var usePrefix = normalized.Length >= MinPrefixMatchLength;
        var prefixPattern = normalized + "%";

        var gated = context.StreetAddresses
            .AsNoTracking()
            .Where(a => cutoff == null || a.LastRefreshed >= cutoff);

        gated = usePrefix
            ? gated.Where(a => EF.Functions.TrigramsAreSimilar(a.SearchText!, normalized)
                || EF.Functions.ILike(a.SearchText!, prefixPattern))
            : gated.Where(a => EF.Functions.TrigramsAreSimilar(a.SearchText!, normalized));

        var candidates = await gated
            .Select(a => new Candidate
            {
                Entity = a,
                TextScore = EF.Functions.TrigramsSimilarity(a.SearchText!, normalized),
                IsPrefixMatch = usePrefix && EF.Functions.ILike(a.SearchText!, prefixPattern),
                DistanceMeters = locationBias == null || a.Location == null ? null : a.Location.Distance(locationBias),
            })
            .Where(c => c.TextScore >= minSimilarity || c.IsPrefixMatch)
            .OrderByDescending(c => c.IsPrefixMatch)
            .ThenByDescending(c => c.TextScore)
            .Take(limit * CandidateMultiplier)
            .ToListAsync(ct);

        // A prefix match is a strong signal regardless of its raw trigram score: grant it a high
        // effective similarity so it both ranks up and clears the cache-hit confidence bar.
        var ranked = candidates
            .Select(c => new
            {
                c.Entity,
                Score = c.IsPrefixMatch ? Math.Max(c.TextScore, PrefixMatchSimilarity) : c.TextScore,
                c.DistanceMeters,
            })
            .OrderByDescending(c => BoostedScore(c.Score, c.DistanceMeters, options))
            .Take(limit);

        foreach (var candidate in ranked)
        {
            yield return new StreetAddressMatch { StreetAddress = candidate.Entity, Similarity = candidate.Score };
        }
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

    /// <summary>
    /// Soft proximity boost: 1.0 at the bias point, decaying toward 0 with distance. Far matches
    /// survive, near ones rank up. Drives ordering only — never returned as the result score.
    /// </summary>
    private static double BoostedScore(double textScore, double? distanceMeters, AddressSearchOptions options)
    {
        if (distanceMeters is not { } distance)
        {
            return textScore;
        }

        var proximity = 1d / (1d + distance / options.BiasScaleMeters);
        return textScore * (1d + options.BiasWeight * proximity);
    }

    /// <summary>Intermediate projection: the materialized candidate plus its raw scoring inputs.</summary>
    private sealed class Candidate
    {
        public required Entities.StreetAddress Entity { get; init; }
        public required double TextScore { get; init; }
        public required bool IsPrefixMatch { get; init; }
        public required double? DistanceMeters { get; init; }
    }
}
