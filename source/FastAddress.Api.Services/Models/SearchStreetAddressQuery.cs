using NetTopologySuite.Geometries;

namespace FastAddress.Api.Services.Models;

/// <summary>
/// Internal street address search query.
/// </summary>
public sealed record SearchStreetAddressQuery
{
    /// <summary>Free-text street address query.</summary>
    public required string Text { get; init; }

    /// <summary>Maximum number of results to return.</summary>
    public required int Limit { get; init; }

    /// <summary>Point used to bias ranking toward nearby results.</summary>
    public required Point? LocationBias { get; init; }
}
