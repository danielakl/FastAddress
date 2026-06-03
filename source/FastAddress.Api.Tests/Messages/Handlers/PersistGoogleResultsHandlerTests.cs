using FastAddress.Api.Database.Models;
using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Services.Messages.Events;
using FastAddress.Api.Services.Messages.Handlers;

using NSubstitute;

namespace FastAddress.Api.Tests.Messages.Handlers;

public sealed class PersistGoogleResultsHandlerTests
{
    private readonly IAddressQueryRepository _queryRepo = Substitute.For<IAddressQueryRepository>();
    private readonly IAddressResultRepository _resultRepo = Substitute.For<IAddressResultRepository>();

    private readonly PersistGoogleResultsHandler _handler;

    public PersistGoogleResultsHandlerTests() => _handler = new PersistGoogleResultsHandler(_queryRepo, _resultRepo);

    [Fact]
    public async Task HandleAsync_StampsLedgerAndUpsertsResults()
    {
        // Arrange - a completed Google fetch carrying one street result.
        var @event = new GoogleResultsRetrieved("LADE ALLE 77", [EventTestBuilders.Upsert(placeId: "street-1")]);

        // Act
        await _handler.HandleAsync(@event, CancellationToken.None);

        // Assert - the ledger is stamped for the exact query and the result is persisted.
        await _queryRepo.Received(1).UpsertAsync("LADE ALLE 77", Arg.Any<CancellationToken>());
        await _resultRepo.Received(1).UpsertRangeAsync(
            Arg.Is<IReadOnlyList<StreetAddressUpsert>>(u => u.Count == 1 && u[0].GooglePlaceId == "street-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmptyResults_StillStampsLedger()
    {
        // Arrange - a query with no street matches must still mark the ledger fresh so we don't
        // re-hammer Google for it within the reuse window.
        var @event = new GoogleResultsRetrieved("NOWHERE STREET", []);

        // Act
        await _handler.HandleAsync(@event, CancellationToken.None);

        // Assert
        await _queryRepo.Received(1).UpsertAsync("NOWHERE STREET", Arg.Any<CancellationToken>());
        await _resultRepo.Received(1).UpsertRangeAsync(
            Arg.Is<IReadOnlyList<StreetAddressUpsert>>(u => u.Count == 0), Arg.Any<CancellationToken>());
    }
}
