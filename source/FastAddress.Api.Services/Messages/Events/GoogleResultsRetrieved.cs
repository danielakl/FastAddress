using FastAddress.Api.Database.Models;

namespace FastAddress.Api.Services.Messages.Events;

/// <summary>
/// Raised after a live Google fetch on the request path has fully streamed to the caller, carrying the
/// normalized query and the street-address upserts derived from it. Handled off the request's critical
/// path to persist the results and stamp the freshness ledger. Published only after the stream drains:
/// a canceled or abandoned fetch never reaches the publish, so a partial fetch cannot mark the ledger
/// fresh and lock out a later full fetch.
/// </summary>
/// <param name="NormalizedQuery">The normalized (upper-cased, single-line) query text.</param>
/// <param name="Results">The street-address upserts to persist (may be empty for a query with no street matches).</param>
public sealed record GoogleResultsRetrieved(
    string NormalizedQuery,
    IReadOnlyList<StreetAddressUpsert> Results) : IDomainEvent
{
    /// <inheritdoc/>
    public string EventType => "address.google-results-retrieved";
}
