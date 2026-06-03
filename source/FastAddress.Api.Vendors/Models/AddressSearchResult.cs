using FastAddress.Api.Vendors.Contracts;
using FastAddress.Api.Vendors.Google.Places.Models;

using NetTopologySuite.Geometries;

namespace FastAddress.Api.Vendors.Models;

/// <summary>
/// One street-address candidate returned from <see cref="IGooglePlacesService"/>.
/// </summary>
public sealed record AddressSearchResult
{
    /// <summary>
    /// Google place identifier.
    /// </summary>
    /// <example>EiRMYWRlIGFsbGU</example>
    public required string PlaceId { get; init; }

    /// <summary>Short, single-line formatted address.</summary>
    /// <example>Lade alle 77, Trondheim</example>
    public required string ShortFormattedAddress { get; init; }

    /// <summary>Geographic location of the address (SRID 4326).</summary>
    public required Point Location { get; init; }

    /// <summary>Place types.</summary>
    /// <example>street_address</example>
    public required IReadOnlyList<string> Types { get; init; }

    /// <summary>Structured address components.</summary>
    /// <example>street, locality, postal code</example>
    public required IReadOnlyList<AddressComponent> AddressComponents { get; init; }
}
