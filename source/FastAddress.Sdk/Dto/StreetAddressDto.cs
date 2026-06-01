using NetTopologySuite.Geometries;

namespace FastAddress.Sdk.Dto;

/// <summary>
/// Street address DTO returned to API consumers.
/// </summary>
public sealed record StreetAddressDto
{
    /// <summary>
    /// Short, single-line formatted street address.
    /// </summary>
    public required string StreetAddress { get; init; }

    /// <summary>
    /// Geographic location of the address (SRID 4326).
    /// </summary>
    public required Point Location { get; init; }

    /// <summary>
    /// Relevance score in 0..1 representing the quality of the search result entry.
    /// </summary>
    public required double Score { get; init; }
}
