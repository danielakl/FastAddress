namespace FastAddress.Api.Vendors.Google.Places.Models;

/// <summary>Geographic coordinate in WGS84.</summary>
public sealed record LatLng
{
    /// <summary>Latitude in degrees, range [-90, 90].</summary>
    public required double Latitude { get; init; }

    /// <summary>Longitude in degrees, range [-180, 180].</summary>
    public required double Longitude { get; init; }
}
