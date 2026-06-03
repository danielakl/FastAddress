namespace FastAddress.Sdk.Exceptions;

/// <summary>
/// Exception that carries everything needed to render an RFC 7807 problem-details response. Lower
/// layers throw it when a failure should surface to the caller as a specific HTTP problem (for example
/// an exhausted upstream quota), and the API host maps it to a problem-details result from the carried
/// <see cref="StatusCode"/>, <see cref="Title"/>, <see cref="Detail"/>, <see cref="Type"/>, and
/// <see cref="Extensions"/>. Keeping it transport-neutral lets any layer raise it without depending on
/// ASP.NET Core types.
/// </summary>
public sealed class ProblemDetailsException : Exception
{
    /// <summary>HTTP status code for the response (for example 429).</summary>
    public int StatusCode { get; }

    /// <summary>Short, human-readable summary of the problem type.</summary>
    public string? Title { get; }

    /// <summary>Human-readable explanation specific to this occurrence.</summary>
    public string? Detail { get; }

    /// <summary>URI reference identifying the problem type, when one applies.</summary>
    public string? Type { get; }

    /// <summary>Additional members to surface on the problem-details payload, when any.</summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>Initializes a new instance of the <see cref="ProblemDetailsException"/> class.</summary>
    /// <param name="statusCode">HTTP status code for the response.</param>
    /// <param name="title">Short summary of the problem type.</param>
    /// <param name="detail">Explanation specific to this occurrence.</param>
    /// <param name="type">URI reference identifying the problem type.</param>
    /// <param name="extensions">Additional problem-details members.</param>
    /// <param name="innerException">The underlying cause, retained for server-side logging.</param>
    public ProblemDetailsException(
        int statusCode,
        string? title = null,
        string? detail = null,
        string? type = null,
        IReadOnlyDictionary<string, object?>? extensions = null,
        Exception? innerException = null)
        : base(detail ?? title, innerException)
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        Type = type;
        Extensions = extensions;
    }
}
