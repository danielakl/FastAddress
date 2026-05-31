namespace FastAddress.Api.Vendors.Google.Places.Models;

/// <summary>
/// Structured component of a place's address. Such as 
/// </summary>
/// <example><code>
/// {
///   "longText": "77",
///   "shortText": "77",
///   "types": [
///     "street_number"
///   ],
///   "languageCode": "nb-NO"
/// }
/// </code></example>
public sealed record AddressComponent
{
    /// <summary>Full, localized text for the component.</summary>
    /// <example>Norge</example>
    public required string LongText { get; init; }

    /// <summary>Short, abbreviated text.</summary>
    /// <example>NO</example>
    public required string ShortText { get; init; }

    /// <summary>Component types.</summary>
    /// <example>street_number, route, postal_town, administrative_area_level_1</example>
    public required IReadOnlyList<string> Types { get; init; }

    /// <summary>BCP-47 language code of the component text.</summary>
    /// <example>nb-NO</example>
    public string? LanguageCode { get; init; }
}
