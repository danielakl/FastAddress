using FastAddress.Api.Database.Entities;
using FastAddress.Sdk.Extensions;

using Microsoft.EntityFrameworkCore;

using NodaTime;

namespace FastAddress.Api.Database.Repositories;

/// <inheritdoc/>
public sealed class AddressQueryRepository(FastAddressDbContext context) : IAddressQueryRepository
{
    // Skip re-stamping a ledger row that was refreshed this recently (mirrors StreetAddressRepository).
    private static readonly Duration RecentRefreshWindow = Duration.FromMinutes(5);

    /// <inheritdoc/>
    public Task<StreetAddressQuery?> FindByQueryAsync(string query, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        return FindByQueryAsync(context.StreetAddressQueries.AsNoTracking(), Normalize(query), ct);
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(string query, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var normalized = Normalize(query);
        var now = context.Clock.GetCurrentInstant();

        // Track the row (no AsNoTracking) so the stamp below updates it in place.
        var entity = await FindByQueryAsync(context.StreetAddressQueries, normalized, ct);
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
            entity = new StreetAddressQuery { Query = normalized };
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

    private static string Normalize(string query) => query.NormalizeSingleLine(toUpperCase: true);
}
