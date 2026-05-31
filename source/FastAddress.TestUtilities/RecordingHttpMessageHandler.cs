using System.Net;

namespace FastAddress.TestUtilities;

/// <summary>
/// Test <see cref="DelegatingHandler"/> that records the request it receives and returns a canned response.
/// Use it as the <see cref="HttpMessageHandler.InnerHandler"/> of a handler under test to assert on the
/// outgoing request the handler produced.
/// </summary>
public sealed class RecordingHttpMessageHandler : DelegatingHandler
{
    private readonly HttpResponseMessage response;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingHttpMessageHandler"/> class.
    /// </summary>
    /// <param name="response">Response to return. Defaults to an empty <see cref="HttpStatusCode.OK"/>.</param>
    public RecordingHttpMessageHandler(HttpResponseMessage? response = null)
    {
        this.response = response ?? new HttpResponseMessage(HttpStatusCode.OK);
    }

    /// <summary>The most recent request that passed through this handler.</summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(response);
    }
}
