using FastAddress.Api.Database.Entities;

namespace FastAddress.Api.Models;

/// <summary>
/// Street address with search metadata.
/// </summary>
public sealed record StreetAddressMatch
{
    /// <summary>Found street address.</summary>
    public required StreetAddress StreetAddress { get; init; }

    /// <summary>Trigram similarity to the query text, between 0..1.</summary>
    public required double Similarity { get; init; }
}
