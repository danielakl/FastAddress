using System.Runtime.CompilerServices;

using FastAddress.Api.Mapping;
using FastAddress.Api.Services;
using FastAddress.Api.Services.Models;
using FastAddress.Sdk.Dto;
using FastAddress.Sdk.Extensions;

using Microsoft.AspNetCore.Mvc;

namespace FastAddress.Api.Controllers;

/// <summary>
/// Street address controller for searching for street addresses.
/// </summary>
[ApiController]
[Route("addresses")]
public sealed class AddressController : ControllerBase
{
    [HttpPost("search")]
    public async IAsyncEnumerable<StreetAddressDto> SearchAddresses(
        [FromBody] SearchStreetAddressDto searchDto,
        [FromServices] IStreetAddressSearchService searchService,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        searchDto = searchDto.CleanAndValidateOrThrow();

        var query = new SearchStreetAddressQuery
        {
            Text = searchDto.Address!,
            Limit = searchDto.Limit!.Value,
            LocationBias = searchDto.LocationBias,
        };

        await foreach (var entry in searchService.Search(query, ct))
        {
            yield return DtoMapper.ToStreetAddressDto(entry);
        }
    }
}
