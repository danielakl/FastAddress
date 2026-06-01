using NodaTime;

namespace FastAddress.Api.Options;

/// <summary>
/// Tuning knobs for cache-first street address search (fuzzy matching, cache-hit policy, spatial bias).
/// </summary>
public sealed class AddressSearchOptions
{
    /// <summary>Expected key to find in the configuration store.</summary>
    public const string ConfigKey = "AddressSearch";

    /// <summary>Candidate floor; also the effective pg_trgm similarity threshold.</summary>
    public double MinSimilarity { get; init; } = 0.30;

    /// <summary>Top hit must clear this similarity to trust the cache.</summary>
    public double ConfidenceThreshold { get; init; } = 0.55;

    /// <summary>Minimum number of candidates required alongside <see cref="ConfidenceThreshold"/>.</summary>
    public int MinConfidentResults { get; init; } = 3;

    /// <summary>A single hit at or above this similarity is enough to serve from cache.</summary>
    public double ExactShortCircuit { get; init; } = 0.85;

    /// <summary>Optional staleness cutoff; <see langword="null"/> means cached rows never expire.</summary>
    public Duration? MaxReuseAge { get; init; }

    /// <summary>Strength of the proximity boost; 0 ignores distance, higher pulls harder toward the bias point.</summary>
    public double BiasWeight { get; init; } = 0.5;

    /// <summary>Decay scale in metres; the proximity boost roughly halves every <see cref="BiasScaleMeters"/>.</summary>
    public double BiasScaleMeters { get; init; } = 2_000d;
}
