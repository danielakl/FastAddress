using FastAddress.Api.Database.Models;
using FastAddress.Api.Database.Options;
using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Services.Mapping;
using FastAddress.Api.Services.Models;
using FastAddress.Api.Services.Options;
using FastAddress.Api.Vendors.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NodaTime;

namespace FastAddress.Api.Services;

/// <inheritdoc/>
public sealed partial class AddressRefreshService(
    IAddressResultRepository addressResultRepo,
    IGooglePlacesService placesService,
    IOptionsMonitor<AddressSearchOptions> searchOptions,
    IOptionsMonitor<AddressRefreshOptions> refreshOptions,
    IClock clock,
    ILogger<AddressRefreshService> logger) : IAddressRefreshService
{
    /// <inheritdoc/>
    public async Task<RefreshStaleResult> RefreshStaleAsync(CancellationToken ct = default)
    {
        var cutoff = clock.GetCurrentInstant() - searchOptions.CurrentValue.MaxReuseAge;
        var refresh = refreshOptions.CurrentValue;

        var stalePlaceIds = await addressResultRepo.FindStalePlaceIdsAsync(cutoff, refresh.BatchSize, ct);
        if (stalePlaceIds.Count == 0)
        {
            return new RefreshStaleResult(Examined: 0, Refreshed: 0, Deleted: 0, Failed: 0, Skipped: 0);
        }

        // The time budget bounds only the Google fetch loop. It is linked to the caller token so a real
        // shutdown still aborts, but on a plain timeout the caller token stays alive to persist progress.
        using var fetchBudget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        fetchBudget.CancelAfter(refresh.RunTimeout.ToTimeSpan());

        var toUpsert = new List<StreetAddressUpsert>();
        var toDelete = new List<string>();
        var attempted = 0;
        var failed = 0;

        foreach (var placeId in stalePlaceIds)
        {
            if (fetchBudget.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var place = await placesService.FetchPlaceAsync(placeId, fetchBudget.Token);
                attempted++;

                // A null place means Google reports the ID as obsolete or returned no usable address.
                // A null mapping means no street line could be derived. Either way the row is dropped.
                var upsert = place is null ? null : ModelMapper.From(place);
                if (upsert is null)
                {
                    toDelete.Add(placeId);
                    continue;
                }

                toUpsert.Add(upsert);

                // A different ID means the place relocated, so the old row is removed and the new one kept.
                if (!string.Equals(upsert.GooglePlaceId, placeId, StringComparison.Ordinal))
                {
                    toDelete.Add(placeId);
                }
            }
            catch (OperationCanceledException) when (fetchBudget.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                // The time budget elapsed mid-fetch. Stop reaching for more and persist what was gathered.
                break;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A transient failure (for example an exhausted quota) leaves the row for a later run.
                attempted++;
                failed++;
                LogRefreshFailed(logger, ex, placeId);
            }
        }

        // Persist with the caller token, which is still alive on a timeout and only canceled on shutdown.
        await addressResultRepo.UpsertRangeAsync(toUpsert, ct);
        await addressResultRepo.DeleteByPlaceIdsAsync(toDelete, ct);

        var result = new RefreshStaleResult(
            Examined: stalePlaceIds.Count,
            Refreshed: toUpsert.Count,
            Deleted: toDelete.Count,
            Failed: failed,
            Skipped: stalePlaceIds.Count - attempted);

        LogRunCompleted(logger, result.Examined, result.Refreshed, result.Deleted, result.Failed, result.Skipped);
        return result;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to refresh place {PlaceId}")]
    private static partial void LogRefreshFailed(ILogger logger, Exception exception, string placeId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stale refresh examined {Examined}, refreshed {Refreshed}, deleted {Deleted}, failed {Failed}, skipped {Skipped}")]
    private static partial void LogRunCompleted(
        ILogger logger, int examined, int refreshed, int deleted, int failed, int skipped);
}
