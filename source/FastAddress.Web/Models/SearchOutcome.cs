using Microsoft.AspNetCore.Mvc;

namespace FastAddress.Web.Models;

/// <summary>
/// Result of a single browser-side address search. Exactly one of <see cref="Results"/> or
/// <see cref="Error"/> is populated. The JS module returns this instead of throwing on an HTTP
/// failure, so the component can surface a clean problem-details message rather than a JS stack trace.
/// </summary>
public sealed record SearchOutcome
{
    /// <summary>The matches when the search succeeded.</summary>
    public IReadOnlyList<AddressResult> Results { get; init; } = [];

    /// <summary>
    /// The failure when the search did not succeed, parsed from the API's
    /// application/problem+json body or built from the HTTP status.
    /// </summary>
    public ProblemDetails? Error { get; init; }
}
