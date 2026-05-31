using System.Runtime.CompilerServices;

using FastAddress.Api.Database;
using FastAddress.Api.Database.Entities;
using FastAddress.Api.Mapping;
using FastAddress.Api.Vendors.Contracts;
using FastAddress.Api.Vendors.Models;
using FastAddress.Sdk.Dto;
using FastAddress.Sdk.Extensions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastAddress.Api.Controllers;

/// <summary>
/// Street address controller for searching for street addresses.
/// </summary>
[ApiController]
[Route("addresses")]
public sealed class AddressController : ControllerBase
{
    [HttpPost("search")]
    public async IAsyncEnumerable<AddressDto> SearchAddressesAsync(
        [FromBody] SearchAddressDto searchDto,
        [FromServices] IGooglePlacesService googlePlacesService,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        searchDto = searchDto.CleanAndValidateOrThrow();

        var searchResult = googlePlacesService.SearchAsync(new AddressSearchRequest
        {
            Query = searchDto.Address!,
            Limit = searchDto.Limit,
        }, ct);

        await foreach (var result in searchResult)
        {
            yield return DtoMapper.ToAddressDto(result);
        }
    }

    [HttpGet]
    public ConfiguredCancelableAsyncEnumerable<StreetAddress> TestGetAllAddresses(
        [FromServices] FastAddressDbContext context,
        CancellationToken ct = default)
    {
        return context.StreetAddresses
            .AsNoTracking()
            .ToAsyncEnumerable()
            .WithCancellation(ct);
    }
}
