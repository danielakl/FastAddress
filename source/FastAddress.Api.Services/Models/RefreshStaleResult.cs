namespace FastAddress.Api.Services.Models;

/// <summary>
/// Summary of a single stale-refresh run.
/// </summary>
/// <param name="Examined">Stale rows pulled for this run.</param>
/// <param name="Refreshed">Rows re-fetched from Google and upserted.</param>
/// <param name="Deleted">Rows removed because Google no longer serves their place ID (or the place relocated).</param>
/// <param name="Failed">Rows left untouched after a transient fetch failure.</param>
/// <param name="Skipped">Rows never reached because the run hit its time budget. These will be picked up on a later run.</param>
public sealed record RefreshStaleResult(int Examined, int Refreshed, int Deleted, int Failed, int Skipped);
