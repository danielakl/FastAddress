namespace FastAddress.Web.Models;

/// <summary>
/// A single search result as returned to the browser and surfaced by the search component.
/// Shaped to match the plain object the <c>AddressSearch.razor.js</c> module hands back
/// (GeoJSON coordinates already flattened to <see cref="Latitude"/>/<see cref="Longitude"/>).
/// </summary>
public sealed record AddressResult
{
    /// <summary>Short, single-line formatted street address.</summary>
    public required string StreetAddress { get; init; }

    /// <summary>Latitude in degrees (WGS84).</summary>
    public double Latitude { get; init; }

    /// <summary>Longitude in degrees (WGS84).</summary>
    public double Longitude { get; init; }

    /// <summary>Relevance score in 0..1.</summary>
    public double Score { get; init; }
}
