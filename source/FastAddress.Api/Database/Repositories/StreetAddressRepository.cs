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

        // The GIN trigram filter is the only hard gate. Pull extra candidate buffer ordered by
        // text similarity, then re-rank by the proximity composite in memory so we stay robust
        // against Npgsql translation limits on the decay arithmetic.
        var candidates = await context.StreetAddresses
            .AsNoTracking()
            .Where(a => EF.Functions.TrigramsAreSimilar(a.SearchText!, normalized))
            .Where(a => cutoff == null || a.LastRefreshed >= cutoff)
            .Select(a => new Candidate
            {
                Entity = a,
                TextScore = EF.Functions.TrigramsSimilarity(a.SearchText!, normalized),
                DistanceMeters = locationBias == null || a.Location == null ? null : a.Location.Distance(locationBias),
            })
            .Where(c => c.TextScore >= minSimilarity)
            .OrderByDescending(c => c.TextScore)
            .Take(limit * CandidateMultiplier)
            .ToListAsync(ct);

        var ranked = candidates
            .OrderByDescending(c => BoostedScore(c.TextScore, c.DistanceMeters, options))
            .Take(limit);

        foreach (var candidate in ranked)
        {
            yield return new StreetAddressMatch { StreetAddress = candidate.Entity, Similarity = candidate.TextScore };
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
        public required double? DistanceMeters { get; init; }
    }
}
