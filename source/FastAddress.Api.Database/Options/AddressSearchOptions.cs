using NodaTime;

namespace FastAddress.Api.Database.Options;

/// <summary>
/// Tuning knobs for cache-first street address search (fuzzy matching, cache-hit policy, spatial bias).
/// </summary>
public sealed class AddressSearchOptions
{
    /// <summary>Expected key to find in the configuration store.</summary>
    public const string ConfigKey = "AddressSearch";

    /// <summary>Candidate floor; also the effective pg_trgm similarity threshold.</summary>
    public double MinSimilarity { get; init; } = 0.30;

    /// <summary>
    /// Freshness window for the <c>StreetAddressQuery</c> ledger: a query refreshed within this window
    /// is served from the database; an older or missing ledger entry triggers a live Google fetch.
    /// </summary>
    public Duration MaxReuseAge { get; init; } = Duration.FromDays(90);

    /// <summary>Decay scale in metres; the proximity boost roughly halves every <see cref="BiasScaleMeters"/>.</summary>
    public double BiasScaleMeters { get; init; } = 2_000d;
}
