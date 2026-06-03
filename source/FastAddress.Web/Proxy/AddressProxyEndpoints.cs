using System.Net.Mime;

using FastAddress.Sdk.Api;
using FastAddress.Sdk.Dto;

using Refit;

namespace FastAddress.Web.Proxy;

/// <summary>
/// Same-origin proxy for the FastAddress API. The browser's <c>fetch</c> (with <c>AbortController</c>)
/// calls these endpoints so search traffic stays same-origin. No CORS needed, and the upstream API base URL
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

        // JSON POST issued by fetch(), not a Blazor form. Antiforgery validation does not apply.
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
        catch (ApiException ae)
        {
            // The upstream API already shaped this failure as problem details (see ProblemDetailsExceptionHandler).
            // Forward its body, content type, and status verbatim so the browser sees the real title, detail, and traceId.
            LogUpstreamFailed(loggerFactory.CreateLogger(typeof(AddressProxyEndpoints)), ae);

            if (!string.IsNullOrEmpty(ae.Content))
            {
                return Results.Content(
                    ae.Content,
                    ae.ContentHeaders?.ContentType?.MediaType ?? MediaTypeNames.Application.ProblemJson,
                    statusCode: (int)ae.StatusCode);
            }

            // The upstream errored without a body to forward. Synthesize a problem-details response.
            return Results.Problem(
                title: "Address search failed",
                detail: "The address service is currently unavailable.",
                statusCode: (int)ae.StatusCode);
        }
        catch (HttpRequestException hre)
        {
            // No HTTP response at all (DNS, connection refused, TLS). Surface a 502 problem-details.
            LogUpstreamFailed(loggerFactory.CreateLogger(typeof(AddressProxyEndpoints)), hre);
            return Results.Problem(
                title: "Address search failed",
                detail: "The address service is currently unavailable.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Address search proxy failed to reach the upstream API")]
    private static partial void LogUpstreamFailed(ILogger logger, Exception exception);
}
