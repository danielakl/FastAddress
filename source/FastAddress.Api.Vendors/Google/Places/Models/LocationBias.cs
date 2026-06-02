namespace FastAddress.Api.Vendors.Google.Places.Models;

/// <summary>
/// Location bias for a Google Places autocomplete request. Biases ranking toward an area. See
/// <a href="https://developers.google.com/maps/documentation/places/web-service/place-autocomplete">Autocomplete</a>.
/// </summary>
public sealed record LocationBias
{
    /// <summary>Circular bias area.</summary>
    public required Circle Circle { get; init; }
}

/// <summary>A circle defined by a center coordinate and a radius in metres (0 .. 50 000).</summary>
public sealed record Circle
{
    /// <summary>Center of the circle.</summary>
    public required LatLng Center { get; init; }

    /// <summary>Radius in metres; Google accepts 0 .. 50 000 inclusive.</summary>
    public required double Radius { get; init; }
}
