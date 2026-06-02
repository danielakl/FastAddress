using NetTopologySuite.Geometries;

namespace FastAddress.Api.Vendors.Models;

/// <summary>
/// Address search request.
/// </summary>
public sealed record AddressSearchRequest
{
    /// <summary>
    /// Query to search for.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Maximum number of results.
    /// </summary>
    public required int? Limit { get; init; }

    /// <summary>
    /// Optional point used to bias ranking toward nearby results.
    /// </summary>
    public Point? LocationBias { get; init; }
}
