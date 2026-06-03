using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Services.Messages.Events;

namespace FastAddress.Api.Services.Messages.Handlers;

/// <summary>
/// Persists the outcome of a live Google Places fetch. Queries and results are recorded and existing records are
/// updated, staleness timestamps are updated.
/// </summary>
internal sealed class PersistGoogleResultsHandler(
    IAddressQueryRepository queryRepo,
    IAddressResultRepository resultRepo) : IDomainEventHandler<GoogleResultsRetrieved>
{
    /// <inheritdoc/>
    public async Task HandleAsync(GoogleResultsRetrieved @event, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@event);

        // Stamp the ledger first: a query that produced no street matches must still be marked fresh so
        // we don't re-hammer Google for it within the reuse window.
        await queryRepo.UpsertAsync(@event.NormalizedQuery, ct);
        await resultRepo.UpsertRangeAsync(@event.Results, ct);
    }
}
