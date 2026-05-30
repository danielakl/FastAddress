using NetTopologySuite.Geometries;

namespace FastAddress.Sdk.Dto;

/// <summary>
/// Street address DTO.
/// </summary>
public sealed record AddressDto
{
    /// <summary>
    /// Street address.
    /// </summary>
    public required string StreetAddress { get; init; }
    
    /// <summary>
    /// Street address.
    /// </summary>
    public required Point Point { get; init; }
    
    /// <summary>
    /// Score representing the quality of the search result entry.
    /// </summary>
    public required double Score { get; init; }
}
