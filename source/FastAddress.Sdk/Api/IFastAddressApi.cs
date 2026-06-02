using FastAddress.Sdk.Dto;

using Refit;

namespace FastAddress.Sdk.Api;

/// <summary>
/// Strongly typed Refit client for the FastAddress API.
/// </summary>
[Headers("Accept: application/json")]
public interface IFastAddressApi
{
    /// <summary>
    /// Search for street addresses matching <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The search query (free text, result limit, optional location bias).</param>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    /// <returns>The matching street addresses, best match first.</returns>
    [Post("/addresses/search")]
    Task<IReadOnlyList<StreetAddressDto>> SearchAddressesAsync(
        [Body] SearchStreetAddressDto request,
        CancellationToken ct = default);
}
