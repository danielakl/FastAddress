using System.Runtime.CompilerServices;

using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Events;
using FastAddress.Api.Models;

namespace FastAddress.Api.Services;

/// <inheritdoc/>
internal sealed class StreetAddressSearchService(
    IStreetAddressRepository addressRepo,
    IDomainEventPublisher events) : IStreetAddressSearchService
{
    /// <inheritdoc/>
    public async IAsyncEnumerable<StreetAddressSearchEntry> Search(
        SearchStreetAddressQuery query,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Serve from the database only. Google is never called on the request path. Count the
        // matches as they stream so the result set stays lazy end to end.
        var count = 0;
        try
        {
            await foreach (var match in addressRepo.Search(query.Text, query.LocationBias, query.Limit).WithCancellation(ct))
            {
                count++;
                yield return ToEntry(match);
            }
        }
        finally
        {
            // The count is known only after the stream drains. Publishing here also means a mid-stream
            // disconnect still warms the cache. The background handler fetches from Google when the
            // match count falls short of the requested limit.
            events.TryPublish(new AddressSearchPerformed(query, count));
        }
    }

    private static StreetAddressSearchEntry ToEntry(StreetAddressMatch match) =>
        new()
        {
            PlaceId = match.StreetAddress.GooglePlaceId,
            StreetLine = match.StreetAddress.StreetLine ?? string.Empty,
            PostalCode = match.StreetAddress.PostalCode,
            PostalTown = match.StreetAddress.PostalTown,
            Location = match.StreetAddress.Location!,
            Score = match.Similarity,
        };
}
