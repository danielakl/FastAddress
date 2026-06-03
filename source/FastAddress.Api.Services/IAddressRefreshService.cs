using FastAddress.Api.Services.Models;

namespace FastAddress.Api.Services;

/// <summary>
/// Refreshes street address results whose data has aged past the reuse window.
/// </summary>
public interface IAddressRefreshService
{
    /// <summary>
    /// Re-fetch a batch of the most stale results from Google and persist the fresh data, deleting rows
    /// whose place IDs Google no longer serves. The run is bounded by a time budget, rows it cannot reach
    /// in time are left for a later run, and whatever was fetched before the budget elapsed is still saved.
    /// </summary>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    /// <returns>A summary of what the run examined and changed.</returns>
    Task<RefreshStaleResult> RefreshStaleAsync(CancellationToken ct = default);
}
