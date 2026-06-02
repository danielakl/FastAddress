using System.Net.Mime;

using FastAddress.Sdk.Api;
using FastAddress.Sdk.Dto;

using Refit;

namespace FastAddress.Web.Proxy;

/// <summary>
/// Same-origin proxy for the FastAddress API. The browser's <c>fetch</c> (with <c>AbortController</c>)
/// calls these endpoints so search traffic stays same-origin — no CORS, and the upstream API base URL
/// is never exposed to the client. Forwarding is done through the strongly typed
/// <see cref="IFastAddressApi"/> Refit client.
/// </summary>
public static partial class AddressProxyEndpoints
{
    private const string SearchPath = "/api/addresses/search";

    /// <summary>
    /// Map the address proxy endpoints onto <paramref name="endpoints"/>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same <paramref name="endpoints"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapAddressProxyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // JSON POST issued by fetch(), not a Blazor form — antiforgery validation does not apply.
        endpoints.MapPost(SearchPath, ForwardSearchAsync).DisableAntiforgery();

        return endpoints;
    }

    private static async Task<IResult> ForwardSearchAsync(
        SearchStreetAddressDto request,
        IFastAddressApi api,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        try
        {
            var results = await api.SearchAddressesAsync(request, ct);
            return Results.Json(results);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException)
        {
            // Upstream unreachable / errored — surface a clean 502 the browser can show in the error modal.
            LogUpstreamFailed(loggerFactory.CreateLogger(typeof(AddressProxyEndpoints)), ex);
            return Results.Json(
                new { error = "The address service is currently unavailable." },
                statusCode: StatusCodes.Status502BadGateway,
                contentType: MediaTypeNames.Application.Json);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Address search proxy failed to reach the upstream API")]
    private static partial void LogUpstreamFailed(ILogger logger, Exception exception);
}
