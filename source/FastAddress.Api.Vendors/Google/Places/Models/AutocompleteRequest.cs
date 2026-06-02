namespace FastAddress.Api.Vendors.Google.Places.Models;

/// <summary>
/// Request body for Google Places API - <c>places:autocomplete</c>.
/// </summary>
public sealed record AutocompleteRequest
{
    /// <summary>Text input to autocomplete. Support fuzzy search "lde ale 77" -> Lade alle 77.</summary>
    public required string Input { get; init; }

    /// <summary>CLDR region codes the results should be limited to.</summary>
    /// <example>["no"]</example>
    public IReadOnlyList<string>? IncludedRegionCodes { get; init; }
    
    /// <summary>Filter for types to return. Can be up to 5 of Table A or B see
    /// <a href="https://developers.google.com/maps/documentation/places/web-service/place-types#about_the_type_tables">Place Types</a>.</summary>
    public IReadOnlyList<string>? IncludedPrimaryTypes { get; init; }
}
