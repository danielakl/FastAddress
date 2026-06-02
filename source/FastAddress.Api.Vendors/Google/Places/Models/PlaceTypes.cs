using System.Collections.Frozen;

namespace FastAddress.Api.Vendors.Google.Places.Models;

/// <summary>
/// Google place type discriminators (the values found in <see cref="PlaceDetailsResponse.Types"/>) that
/// represent a usable street-level address. Anything outside this set (for example a bare
/// <c>locality</c> such as "Trondheim") is not an address we can store or return.
/// See <a href="https://developers.google.com/maps/documentation/places/web-service/place-types">Place types</a>.
/// </summary>
public static class PlaceTypes
{
    /// <summary>A precise street address.</summary>
    /// <example>Lade alle 77</example>
    public const string StreetAddress = "street_address";

    /// <summary>A named building or location.</summary>
    /// <example>Lade alle 80</example>
    public const string Premise = "premise";

    /// <summary>An addressable unit within a <see cref="Premise"/>, such as an apartment.</summary>
    /// <example>Lade alle 77 a</example>
    public const string Subpremise = "subpremise";

    /// <summary>
    /// The place types accepted as street-level addresses, both as the autocomplete request restriction
    /// and the response filter. Results of any other type are dropped.
    /// </summary>
    public static readonly IReadOnlySet<string> StreetAddressTypes =
        FrozenSet.Create(StringComparer.OrdinalIgnoreCase, StreetAddress, Premise, Subpremise);

    /// <summary>Whether the given Google place type counts as a street-level address.</summary>
    /// <param name="type">A value from <see cref="PlaceDetailsResponse.Types"/>.</param>
    /// <returns><see langword="true"/> when the type is a street-level address type.</returns>
    public static bool IsStreetAddressType(string type) => StreetAddressTypes.Contains(type);
}
