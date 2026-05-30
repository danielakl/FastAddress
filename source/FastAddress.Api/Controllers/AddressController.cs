using System.Runtime.CompilerServices;

using FastAddress.Api.Database;
using FastAddress.Api.Database.Entities;
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
    public async Task<SearchAddressDto> SearchAddressesAsync(
        [FromBody] SearchAddressDto searchDto,
        CancellationToken ct = default)
    {
        searchDto = searchDto.CleanAndValidateOrThrow();
        
        return searchDto;
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
