using System.Net.Mime;
using System.Text.Json;

using FastAddress.Sdk.Exceptions;
using FastAddress.Sdk.Serialization;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FastAddress.Api.Infrastructure;

/// <summary>
/// Maps a <see cref="ProblemDetailsException"/> thrown anywhere below the host into an RFC 7807
/// problem-details response, using the status, title, detail, type, and extensions it carries. The
/// payload is built through <see cref="ProblemDetailsFactory"/> so it picks up the conventional
/// defaults (status-to-title mapping, <c>traceId</c>). Any other exception is left for the default handler.
/// </summary>
internal sealed class ProblemDetailsExceptionHandler(ProblemDetailsFactory problemDetailsFactory)
    : IExceptionHandler
{
    // Reuse the shared FastAddress serializer settings so problem-details payloads match every other response.
    private static readonly JsonSerializerOptions JsonOptions = FastAddressJsonOptions.Create();

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not ProblemDetailsException problem)
        {
            return false;
        }

        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            httpContext,
            statusCode: problem.StatusCode,
            title: problem.Title,
            type: problem.Type,
            detail: problem.Detail);

        if (problem.Extensions is not null)
        {
            foreach (var (key, value) in problem.Extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        httpContext.Response.StatusCode = problem.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            JsonOptions,
            contentType: MediaTypeNames.Application.ProblemJson,
            cancellationToken: cancellationToken);

        return true;
    }
}
