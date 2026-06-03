using FastAddress.Api.Database.Models;
using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Services.Messages.Events;
using FastAddress.Api.Services.Messages.Handlers;
using FastAddress.Api.Vendors.Contracts;
using FastAddress.Api.Vendors.Models;
using FastAddress.TestUtilities;

using NSubstitute;

namespace FastAddress.Api.Tests.Messages.Handlers;

public sealed class AddressSearchEventHandlerTests
{
    private readonly IGooglePlacesService _placesService = Substitute.For<IGooglePlacesService>();
    private readonly IStreetAddressRepository _streetAddressRepo = Substitute.For<IStreetAddressRepository>();

    private readonly AddressSearchEventHandler _eventHandler;

    public AddressSearchEventHandlerTests() => _eventHandler = new AddressSearchEventHandler(_placesService, _streetAddressRepo);

    private void MockAddressSearch(params AddressSearchResult[] results) =>
        _placesService.Search(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(results.AsAsyncEnumerable());

    [Fact]
    public async Task HandleAsync_MatchCountReachesLimit_DoesNotCallGoogle()
    {
        // Arrange - the cache already returned a full page (5 of 5).
        var @event = new AddressSearchPerformed(EventTestBuilders.Query(limit: 5), MatchCount: 5);

        // Act
        await _eventHandler.HandleAsync(@event, CancellationToken.None);

        // Assert
        _placesService.DidNotReceive().Search(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>());
        await _streetAddressRepo.DidNotReceive().UpsertRangeAsync(
            Arg.Any<IReadOnlyList<StreetAddressUpsert>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FewerMatchesThanLimit_FetchesGoogleAndUpserts()
    {
        // Arrange - 1 cached match for a limit of 5 is insufficient, so Google is consulted.
        MockAddressSearch(EventTestBuilders.GoogleResult("street-1"));
        var @event = new AddressSearchPerformed(EventTestBuilders.Query(limit: 5), MatchCount: 1);

        // Act
        await _eventHandler.HandleAsync(@event, CancellationToken.None);

        // Assert
        _placesService.Received(1).Search(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>());
        await _streetAddressRepo.Received(1).UpsertRangeAsync(
            Arg.Is<IReadOnlyList<StreetAddressUpsert>>(u => u.Count == 1 && u[0].GooglePlaceId == "street-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DropsNonStreetAddressResultsBeforeUpsert()
    {
        // Arrange - Google returns a bare locality alongside a street address; only the latter persists.
        MockAddressSearch(
            EventTestBuilders.GoogleResult("locality-0", type: "locality"),
            EventTestBuilders.GoogleResult("street-1", type: "street_address"));
        var @event = new AddressSearchPerformed(EventTestBuilders.Query(limit: 5), MatchCount: 0);

        // Act
        await _eventHandler.HandleAsync(@event, CancellationToken.None);

        // Assert
        await _streetAddressRepo.Received(1).UpsertRangeAsync(
            Arg.Is<IReadOnlyList<StreetAddressUpsert>>(u => u.Count == 1 && u[0].GooglePlaceId == "street-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ForwardsQueryTextAndBiasToGoogle()
    {
        // Arrange - the query the user searched must reach the vendor request unchanged.
        var bias = GeoTestData.Point(10.4, 63.4);
        MockAddressSearch(EventTestBuilders.GoogleResult("street-1"));
        var query = EventTestBuilders.Query(text: "Lade alle 77", limit: 5, locationBias: bias);
        var @event = new AddressSearchPerformed(query, MatchCount: 0);

        // Act
        await _eventHandler.HandleAsync(@event, CancellationToken.None);

        // Assert
        _placesService.Received(1).Search(
            Arg.Is<AddressSearchRequest>(r => r.Query == "Lade alle 77" && r.LocationBias == bias),
            Arg.Any<CancellationToken>());
    }
}
