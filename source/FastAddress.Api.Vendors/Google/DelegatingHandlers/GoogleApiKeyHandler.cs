using FastAddress.Api.Vendors.Options;

using Microsoft.Extensions.Options;

namespace FastAddress.Api.Vendors.Google.DelegatingHandlers;

/// <summary>
/// Adds Google's API key header to every outgoing Google API request.
/// </summary>
internal sealed class GoogleApiKeyHandler(IOptionsMonitor<GoogleApisOptions> options) : DelegatingHandler
{
    private const string ApiKeyHeader = "X-Goog-Api-Key";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Remove(ApiKeyHeader);
        request.Headers.Add(ApiKeyHeader, options.CurrentValue.ApiKey);

        return base.SendAsync(request, cancellationToken);
    }
}
