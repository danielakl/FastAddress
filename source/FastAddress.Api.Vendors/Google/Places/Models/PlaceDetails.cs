namespace FastAddress.Api.Vendors.Google.Places.Models;

/// <summary>
/// Response from Google Places Details API endpoint.
/// </summary>
/// <remarks>Field mask: <c>id,movedPlaceId,addressComponents,shortFormattedAddress,location,types</c>.</remarks>
public sealed record PlaceDetailsResponse
{
    /// <summary>Place identifier.</summary>
    /// <example>EiRMYWRlIGFsbGU</example>
    public required string Id { get; init; }

    /// <summary>
    /// If this place has been replaced, the identifier of the replacement.
    /// Populated only when Google merges/relocates a place.
    /// </summary>
    /// <example>EiRMYWRlIGFsbGU</example>
    public string? MovedPlaceId { get; init; }

    /// <summary>Structured address components.</summary>
    public IReadOnlyList<AddressComponent>? AddressComponents { get; init; }

    /// <summary>Short, single-line formatted address.</summary>
    /// <example>Lade alle 77, Trondheim</example>
    public string? ShortFormattedAddress { get; init; }

    /// <summary>Geographic coordinate of the place.</summary>
    public LatLng? Location { get; init; }

    /// <summary>Place types.</summary>
    /// <example>["street_address"]</example>
    public IReadOnlyList<string>? Types { get; init; }
}
