using FastAddress.Api.Vendors.Models;
using FastAddress.Sdk.Dto;

namespace FastAddress.Api.Mapping;

/// <summary>
/// Mapping between internal types and SDK DTOs.
/// </summary>
internal static class DtoMapper
{
    /// <summary>
    /// Project an <see cref="AddressSearchResult"/> onto the <see cref="AddressDto"/>.
    /// </summary>
    public static AddressDto ToAddressDto(AddressSearchResult result)
    {
        return new AddressDto
        {
            StreetAddress = result.ShortFormattedAddress,
            Point = result.Location,
            Score = 1d / (result.OrderScore + 1), // TODO: Look at score calculation
        };
    }
}
