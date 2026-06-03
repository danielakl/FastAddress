using FastAddress.Api.Database.Models;
using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Services.Mapping;
using FastAddress.Api.Services.Messages.Events;
using FastAddress.Api.Services.Models;
using FastAddress.Api.Vendors.Contracts;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;

namespace FastAddress.Api.Services.Messages.Handlers;

/// <summary>
/// Warms the cache in the background: when a search returned fewer matches than requested, fetch the
/// query from Google Places, keep only street-address results, and bulk-upsert
/// them to the database. Confident searches that already filled the limit cost no Google call.
/// </summary>
internal sealed class AddressSearchEventHandler(
    IGooglePlacesService placesService,
    IStreetAddressRepository repo) : IDomainEventHandler<AddressSearchPerformed>
{
    /// <inheritdoc/>
    public async Task HandleAsync(AddressSearchPerformed @event, CancellationToken ct)
    {
        var query = @event.Query;
        if (@event.MatchCount >= query.Limit)
        {
            return; // The cache already satisfied the request; don't spend a Google call.
        }

        var upserts = new List<StreetAddressUpsert>();
        await foreach (var result in placesService.Search(ToVendorRequest(query), ct))
        {
            if (!IsStreetAddress(result))
            {
                continue; // Drop non-address results (e.g. a city/locality) before storing.
            }

            var upsert = ModelMapper.From(result);
            if (upsert is not null)
            {
                upserts.Add(upsert);
            }
        }

        await repo.UpsertRangeAsync(upserts, ct);
    }

    private static bool IsStreetAddress(AddressSearchResult result) =>
        result.Types.Any(PlaceTypes.IsStreetAddressType);

    private static AddressSearchRequest ToVendorRequest(SearchStreetAddressQuery query) =>
        new() { Query = query.Text, Limit = query.Limit, LocationBias = query.LocationBias };
}
