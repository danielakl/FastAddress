namespace FastAddress.Api.Vendors.Google.Places.Models;

/// <summary>
/// Google address component type discriminators (the values found in <see cref="AddressComponent.Types"/>).
/// See <a href="https://developers.google.com/maps/documentation/places/web-service/place-types">Place types</a>.
/// </summary>
public static class AddressComponentTypes
{
    /// <summary>Street name.</summary>
    /// <example>Lade alle</example>
    public const string Route = "route";

    /// <summary>Street number (may include a house letter).</summary>
    /// <example>77 a</example>
    public const string StreetNumber = "street_number";

    /// <summary>Named building or location. Used as the street line when route/number are absent.</summary>
    /// <example>Trondheim Lufthavn Værnes</example>
    public const string Premise = "premise";

    /// <summary>Postal code.</summary>
    /// <example>7041</example>
    public const string PostalCode = "postal_code";

    /// <summary>Postal town (preferred locality for postal addressing).</summary>
    /// <example>Trondheim</example>
    public const string PostalTown = "postal_town";

    /// <summary>Locality (city); fallback when <see cref="PostalTown"/> is absent.</summary>
    /// <example>Trondheim</example>
    public const string Locality = "locality";

    /// <summary>Country.</summary>
    /// <example>Norge</example>
    public const string Country = "country";
}
