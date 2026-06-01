using NetTopologySuite.Geometries;

namespace FastAddress.Api.Models;

/// <summary>
/// Street address search entry.
/// </summary>
public sealed record StreetAddressSearchEntry
{
    /// <summary>Google place identifier.</summary>
    public required string PlaceId { get; init; }

    /// <summary>Single-line street address text.</summary>
    /// <example>Lade allé 77 a</example>
    public required string StreetLine { get; init; }

    /// <summary>Geographic location of the address (SRID 4326).</summary>
    public required Point Location { get; init; }

    /// <summary>
    /// A relevance score (0..1). Google hits are derived from result order (1/(rank+1)).
    /// Cache hits are scored by trigram similarity. The proximity bias affects ordering only and is
    /// deliberately not folded into this value, keeping it bounded in 0..1 and source-comparable.
    /// </summary>
    public required double Score { get; init; }

    /// <summary>Whether the result is from cache or fetched from Google Places.</summary>
    public required bool IsCacheHit { get; init; }
}
