using System.Diagnostics.CodeAnalysis;

using FastAddress.Api.Models;
using FastAddress.Sdk.Dto;

namespace FastAddress.Api.Mapping;

/// <summary>
/// Mapping between internal types and SDK DTOs.
/// </summary>
internal static class DtoMapper
{
    /// <summary>
    /// Project a <see cref="StreetAddressSearchEntry"/> onto the <see cref="StreetAddressDto"/>.
    /// </summary>
    [return:NotNullIfNotNull(nameof(entry))]
    public static StreetAddressDto? ToStreetAddressDto(StreetAddressSearchEntry? entry)
    {
        if (entry is null)
        {
            return null;
        }

        return new StreetAddressDto
        {
            StreetAddress = entry.StreetLine,
            Location = entry.Location,
            Score = entry.Score,
        };
    }
}
