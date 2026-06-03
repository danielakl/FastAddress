using System.Runtime.CompilerServices;

using FastAddress.Api.Database.Models;
using FastAddress.Api.Database.Options;
using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Services.Mapping;
using FastAddress.Api.Services.Messages;
using FastAddress.Api.Services.Messages.Events;
using FastAddress.Api.Services.Models;
using FastAddress.Api.Vendors.Contracts;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;
using FastAddress.Sdk.Extensions;

using Microsoft.Extensions.Options;

using NodaTime;

namespace FastAddress.Api.Services;

/// <inheritdoc/>
public sealed class AddressSearchService(
    IAddressQueryRepository queryRepo,
    IAddressResultRepository addressRepo,
    IGooglePlacesService placesService,
    IDomainEventPublisher eventPublisher,
    IOptionsMonitor<AddressSearchOptions> options,
    IClock clock) : IAddressSearchService
{
    /// <inheritdoc/>
    public async IAsyncEnumerable<StreetAddressSearchEntry> Search(
        SearchStreetAddressQuery query,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalized = query.Text.NormalizeSingleLine(toUpperCase: true);
        var ledger = await queryRepo.FindByQueryAsync(normalized, ct);

        var isFresh = ledger is not null
            && ledger.LastRefreshed >= clock.GetCurrentInstant() - options.CurrentValue.MaxReuseAge;

        if (isFresh)
        {
            // The ledger says this query was fetched recently: serve the cache, never call Google.
            await foreach (var match in addressRepo
                .Search(query.Text, query.LocationBias, query.Limit)
                .WithCancellation(ct))
            {
                yield return ToEntry(match);
            }

            yield break;
        }

        // Ledger missing or stale: fetch from Google so the caller always gets current data.
        var upserts = new List<StreetAddressUpsert>();
        await foreach (var result in placesService.SearchPlaces(ToVendorRequest(query), ct))
        {
            if (!IsStreetAddress(result))
            {
                continue; // Drop non-address results (e.g. a city/locality) before serving or storing.
            }

            var upsert = ModelMapper.From(result);
            if (upsert is null)
            {
                continue; // No street line could be derived.
            }

            upserts.Add(upsert);
            yield return ToEntry(upsert);
        }

        // Publish AFTER the stream drains. A canceled or abandoned stream throws or
        // suspends before here, so a partial fetch never marks the ledger fresh. An empty-but-completed
        // fetch still publishes, marking the query fresh so we don't re-hammer Google for it.
        eventPublisher.TryPublish(new GoogleResultsRetrieved(normalized, upserts));
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

    private static StreetAddressSearchEntry ToEntry(StreetAddressUpsert upsert) =>
        new()
        {
            PlaceId = upsert.GooglePlaceId,
            StreetLine = upsert.StreetLine,
            PostalCode = upsert.PostalCode,
            PostalTown = upsert.PostalTown,
            Location = upsert.Location,
            // Straight from Google, so there is no trigram similarity to report.
            Score = null,
        };

    private static bool IsStreetAddress(GooglePlace result) =>
        result.Types.Any(PlaceTypes.IsStreetAddressType);

    private static AddressSearchRequest ToVendorRequest(SearchStreetAddressQuery query) =>
        new() { Query = query.Text, Limit = query.Limit, LocationBias = query.LocationBias };
}
