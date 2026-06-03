// ReSharper disable always EntityFramework.ModelValidation.UnlimitedStringLength - Configured with fluent syntax in DB context.

using NetTopologySuite.Geometries;

using NodaTime;

namespace FastAddress.Api.Database.Entities;

/// <summary>
/// Street address result entity with support for fuzzy search through <see cref="SearchText"/>.
/// </summary>
public sealed class StreetAddressResult : IEntityTimestamps
{
    /// <summary>
    /// Identifier.
    /// </summary>
    public long Id { get; set; }

    /// <inheritdoc/>
    public Instant Added { get; set; }

    /// <inheritdoc/>
    public Instant Modified { get; set; }

    /// <summary>Google place identifier.</summary>
    public required string GooglePlaceId { get; set; }

    /// <summary>Street line composed of the steet name and number.</summary>
    /// <example>Lade Allé 77 A</example>
    public string? StreetLine { get; set; }

    /// <summary>Postal code.</summary>
    /// <example>7041</example>
    public string? PostalCode { get; set; }

    /// <summary>Postal town or locality.</summary>
    /// <example>Trondheim</example>
    public string? PostalTown { get; set; }

    /// <summary>Country.</summary>
    /// <example>Norway</example>
    public string? Country { get; set; }

    /// <summary>Normalized version <see cref="StreetLine"/> used for trigram search.</summary>
    /// <example>LADE ALLÉ 77 A</example>
    public string? SearchText { get; set; }

    /// <summary>Geographic location, stored as geography(Point,4326) to enable distance indexing.</summary>
    public Point? Location { get; set; }

    /// <summary>Timestamp of the last refresh from Google. Used to determine staleness via MaxReuseAge.</summary>
    public Instant LastRefreshed { get; set; }
}
