using FastAddress.Api.Database.Entities;
using FastAddress.Sdk.Extensions;

using Microsoft.EntityFrameworkCore;

using NodaTime;

namespace FastAddress.Api.Database.Repositories;

/// <inheritdoc/>
public sealed class AddressQueryRepository(FastAddressDbContext context) : IAddressQueryRepository
{
    // Skip re-stamping a ledger row that was refreshed this recently.
    private static readonly Duration RecentRefreshWindow = Duration.FromMinutes(5);

    /// <inheritdoc/>
    public async Task<StreetAddressQuery?> FindByQueryAsync(string query, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var normalizedQuery = query.NormalizeSingleLine(toUpperCase: true);
        return await FindByQueryAsync(context.StreetAddressQueries.AsNoTracking(), normalizedQuery, ct);
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(string query, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var normalizedQuery = query.NormalizeSingleLine(toUpperCase: true);
        var now = context.Clock.GetCurrentInstant();

        // Track the row (no AsNoTracking) so the stamp below updates it in place.
        var entity = await FindByQueryAsync(context.StreetAddressQueries, normalizedQuery, ct);
        if (entity is not null)
        {
            // Leave a row refreshed within the window untouched rather than bumping it again.
            if (entity.LastRefreshed >= now - RecentRefreshWindow)
            {
                return;
            }
        }
        else
        {
            entity = new StreetAddressQuery { Query = normalizedQuery };
            context.StreetAddressQueries.Add(entity);
        }

        entity.LastRefreshed = now;

        await context.SaveChangesAsync(ct);
    }

    // The Query column is uniquely indexed, so at most one row can match.
    private static Task<StreetAddressQuery?> FindByQueryAsync(
        IQueryable<StreetAddressQuery> source,
        string normalizedQuery,
        CancellationToken ct = default) =>
        source.SingleOrDefaultAsync(q => q.Query == normalizedQuery, ct);
}
