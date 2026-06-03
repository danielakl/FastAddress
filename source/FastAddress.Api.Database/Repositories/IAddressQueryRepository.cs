using FastAddress.Api.Database.Entities;

namespace FastAddress.Api.Database.Repositories;

/// <summary>
/// Repository for the <see cref="StreetAddressQuery"/> freshness ledger. A ledger of the queries that has been sent to
/// Google Places API and when.
/// </summary>
public interface IAddressQueryRepository
{
    /// <summary>
    /// Find the ledger row for a query, if one exists.
    /// </summary>
    /// <param name="query">Query text.</param>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    /// <returns>The ledger row, or <see langword="null"/> when the query has never been fetched.</returns>
    Task<StreetAddressQuery?> FindByQueryAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Record a successful Google fetch for a query. Inserted if missing, or <see cref="StreetAddressQuery.LastRefreshed"/>
    /// is updated if found.
    /// </summary>
    /// <param name="query">Query text.</param>
    /// <param name="ct">Cancellation token for canceling the ongoing operation.</param>
    Task UpsertAsync(string query, CancellationToken ct = default);
}
