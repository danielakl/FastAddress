using NetTopologySuite.Geometries;

namespace FastAddress.Api.Services.Models;

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

    /// <summary>Postal code, when known.</summary>
    /// <example>7041</example>
    public string? PostalCode { get; init; }

    /// <summary>Postal town, when known.</summary>
    /// <example>Trondheim</example>
    public string? PostalTown { get; init; }

    /// <summary>Geographic location of the address (SRID 4326).</summary>
    public required Point Location { get; init; }

    /// <summary>
    /// Trigram-similarity relevance score (0..1) for database matches. <see langword="null"/> for
    /// results served straight from Google, where no similarity was computed.
    /// </summary>
    public required double? Score { get; init; }
}
