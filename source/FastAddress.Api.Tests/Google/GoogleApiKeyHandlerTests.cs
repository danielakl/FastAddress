using FastAddress.Api.Vendors.Google.DelegatingHandlers;
using FastAddress.Api.Vendors.Options;
using FastAddress.TestUtilities;

using Microsoft.Extensions.Options;

using NSubstitute;

namespace FastAddress.Api.Tests.Google;

public sealed class GoogleApiKeyHandlerTests
{
    private const string ApiKeyHeader = "X-Goog-Api-Key";

    private static (GoogleApiKeyHandler handler, RecordingHttpMessageHandler inner) CreateHandler(string apiKey)
    {
        var options = Substitute.For<IOptionsMonitor<GoogleApisOptions>>();
        options.CurrentValue.Returns(new GoogleApisOptions { ApiKey = apiKey });

        var inner = new RecordingHttpMessageHandler();
        var handler = new GoogleApiKeyHandler(options) { InnerHandler = inner };
        return (handler, inner);
    }

    [Fact]
    public async Task SendAsync_RequestWithoutApiKey_AddsApiKeyHeaderFromOptions()
    {
        // Arrange
        var (handler, inner) = CreateHandler("secret-key");
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://places.googleapis.com/v1/places/x");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("secret-key", Assert.Single(inner.LastRequest!.Headers.GetValues(ApiKeyHeader)));
    }

    [Fact]
    public async Task SendAsync_RequestWithExistingApiKey_ReplacesItWithConfiguredKey()
    {
        // Arrange
        var (handler, inner) = CreateHandler("new-key");
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://places.googleapis.com/v1/places/x");
        request.Headers.Add(ApiKeyHeader, "stale-key");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("new-key", Assert.Single(inner.LastRequest!.Headers.GetValues(ApiKeyHeader)));
    }
}
