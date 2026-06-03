using NetTopologySuite.Geometries;

namespace FastAddress.Api.Database.Models;

/// <summary>
/// Upsertable street address payload.
/// </summary>
public sealed record StreetAddressUpsert
{
    /// <summary>Unique Google place ID.</summary>
    public required string GooglePlaceId { get; init; }

    /// <summary>Street line composed of route and street number.</summary>
    /// <example>Lade Allé 77 A</example>
    public required string StreetLine { get; init; }

    /// <summary>Postal code.</summary>
    /// <example>7041</example>
    public string? PostalCode { get; init; }

    /// <summary>Postal town or locality.</summary>
    /// <example>Trondheim</example>
    public string? PostalTown { get; init; }

    /// <summary>Country.</summary>
    /// <example>Norway</example>
    public string? Country { get; init; }

    /// <summary>Normalized <see cref="StreetLine"/> used for trigram search.</summary>
    public required string SearchText { get; init; }

    /// <summary>Geographic location (SRID 4326).</summary>
    public required Point Location { get; init; }
}
