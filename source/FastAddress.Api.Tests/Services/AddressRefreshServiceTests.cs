using FastAddress.Api.Database.Models;
using FastAddress.Api.Database.Options;
using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Services;
using FastAddress.Api.Services.Models;
using FastAddress.Api.Services.Options;
using FastAddress.Api.Tests.Messages;
using FastAddress.Api.Vendors.Contracts;
using FastAddress.Api.Vendors.Models;
using FastAddress.Sdk.Exceptions;
using FastAddress.TestUtilities;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NodaTime;
using NodaTime.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FastAddress.Api.Tests.Services;

public sealed class AddressRefreshServiceTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 6, 3, 12, 0, 0);
    private static readonly Duration MaxReuseAge = Duration.FromDays(90);
    private const int BatchSize = 100;

    private readonly IAddressResultRepository _addressResultRepo = Substitute.For<IAddressResultRepository>();
    private readonly IGooglePlacesService _placesService = Substitute.For<IGooglePlacesService>();
    private readonly IOptionsMonitor<AddressSearchOptions> _searchOptions = Substitute.For<IOptionsMonitor<AddressSearchOptions>>();
    private readonly IOptionsMonitor<AddressRefreshOptions> _refreshOptions = Substitute.For<IOptionsMonitor<AddressRefreshOptions>>();
    private readonly FakeClock _clock = new(Now);

    private readonly AddressRefreshService _service;

    public AddressRefreshServiceTests()
    {
        _searchOptions.CurrentValue.Returns(new AddressSearchOptions { MaxReuseAge = MaxReuseAge });
        _refreshOptions.CurrentValue.Returns(new AddressRefreshOptions { BatchSize = BatchSize });
        _service = new AddressRefreshService(
            _addressResultRepo, _placesService, _searchOptions, _refreshOptions, _clock, NullLogger<AddressRefreshService>.Instance);
    }

    private void MockStale(params string[] placeIds) =>
        _addressResultRepo.FindStalePlaceIdsAsync(Arg.Any<Instant>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(placeIds);

    private void MockFetch(string placeId, GooglePlace? place) =>
        _placesService.FetchPlaceAsync(placeId, Arg.Any<CancellationToken>()).Returns(place);

    // A place with no route/street-number/premise components, so ModelMapper derives no street line.
    private static GooglePlace PlaceWithoutStreetLine(string placeId) =>
        new()
        {
            PlaceId = placeId,
            ShortFormattedAddress = "Trondheim",
            Location = GeoTestData.Point(10.0, 63.0),
            Types = ["street_address"],
            AddressComponents = [],
        };

    [Fact]
    public async Task RefreshStaleAsync_NoStaleRows_ReturnsZeroSummaryAndTouchesNothing()
    {
        // Arrange
        MockStale();

        // Act
        var result = await _service.RefreshStaleAsync();

        // Assert
        Assert.Equal(new RefreshStaleResultTuple(0, 0, 0, 0, 0), Tuple(result));
        await _placesService.DidNotReceive().FetchPlaceAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _addressResultRepo.DidNotReceive().UpsertRangeAsync(Arg.Any<IReadOnlyList<StreetAddressUpsert>>(), Arg.Any<CancellationToken>());
        await _addressResultRepo.DidNotReceive().DeleteByPlaceIdsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshStaleAsync_QueriesStaleRowsUsingMaxReuseAgeCutoffAndBatchSize()
    {
        // Arrange - An empty result still proves which cutoff and limit the service asked for.
        MockStale();

        // Act
        await _service.RefreshStaleAsync();

        // Assert - Cutoff is the current instant minus the configured reuse window.
        await _addressResultRepo.Received(1).FindStalePlaceIdsAsync(Now - MaxReuseAge, BatchSize, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshStaleAsync_StaleRowRefreshed_UpsertsMappedResultAndDeletesNothing()
    {
        // Arrange - Google returns fresh details for the stale place.
        MockStale("place-0");
        MockFetch("place-0", EventTestBuilders.GoogleResult("place-0", streetName: "Lade alle", streetNumber: "77"));

        // Act
        var result = await _service.RefreshStaleAsync();

        // Assert
        await _addressResultRepo.Received(1).UpsertRangeAsync(
            Arg.Is<IReadOnlyList<StreetAddressUpsert>>(l => l.Count == 1 && l[0].GooglePlaceId == "place-0"),
            Arg.Any<CancellationToken>());
        await _addressResultRepo.Received(1).DeleteByPlaceIdsAsync(
            Arg.Is<IReadOnlyList<string>>(l => l.Count == 0), Arg.Any<CancellationToken>());
        Assert.Equal(new RefreshStaleResultTuple(1, 1, 0, 0, 0), Tuple(result));
    }

    [Fact]
    public async Task RefreshStaleAsync_ObsoletePlace_FetchReturnsNull_DeletesRow()
    {
        // Arrange - Google reports the ID as obsolete (FetchPlaceAsync returns null).
        MockStale("place-0");
        MockFetch("place-0", null);

        // Act
        var result = await _service.RefreshStaleAsync();

        // Assert
        await _addressResultRepo.Received(1).DeleteByPlaceIdsAsync(
            Arg.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "place-0"), Arg.Any<CancellationToken>());
        await _addressResultRepo.Received(1).UpsertRangeAsync(
            Arg.Is<IReadOnlyList<StreetAddressUpsert>>(l => l.Count == 0), Arg.Any<CancellationToken>());
        Assert.Equal(new RefreshStaleResultTuple(1, 0, 1, 0, 0), Tuple(result));
    }

    [Fact]
    public async Task RefreshStaleAsync_DetailsWithNoStreetLine_DeletesRow()
    {
        // Arrange - The place still resolves, but no street line can be derived from its components.
        MockStale("place-0");
        MockFetch("place-0", PlaceWithoutStreetLine("place-0"));

        // Act
        var result = await _service.RefreshStaleAsync();

        // Assert
        await _addressResultRepo.Received(1).DeleteByPlaceIdsAsync(
            Arg.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "place-0"), Arg.Any<CancellationToken>());
        Assert.Equal(new RefreshStaleResultTuple(1, 0, 1, 0, 0), Tuple(result));
    }

    [Fact]
    public async Task RefreshStaleAsync_RelocatedPlace_UpsertsNewIdAndDeletesOldId()
    {
        // Arrange - The fetched place carries a different ID, meaning Google relocated it.
        MockStale("place-old");
        MockFetch("place-old", EventTestBuilders.GoogleResult("place-new"));

        // Act
        var result = await _service.RefreshStaleAsync();

        // Assert - The replacement is stored and the superseded row removed.
        await _addressResultRepo.Received(1).UpsertRangeAsync(
            Arg.Is<IReadOnlyList<StreetAddressUpsert>>(l => l.Count == 1 && l[0].GooglePlaceId == "place-new"),
            Arg.Any<CancellationToken>());
        await _addressResultRepo.Received(1).DeleteByPlaceIdsAsync(
            Arg.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "place-old"), Arg.Any<CancellationToken>());
        Assert.Equal(new RefreshStaleResultTuple(1, 1, 1, 0, 0), Tuple(result));
    }

    [Fact]
    public async Task RefreshStaleAsync_TransientFailure_LeavesRowAndKeepsProcessingOthers()
    {
        // Arrange - The first lookup hits an exhausted quota and the second succeeds.
        MockStale("bad", "good");
        _placesService.FetchPlaceAsync("bad", Arg.Any<CancellationToken>())
            .ThrowsAsync(new ProblemDetailsException(statusCode: 429));
        MockFetch("good", EventTestBuilders.GoogleResult("good"));

        // Act
        var result = await _service.RefreshStaleAsync();

        // Assert - Only the good place is upserted; the failed one is neither upserted nor deleted.
        await _addressResultRepo.Received(1).UpsertRangeAsync(
            Arg.Is<IReadOnlyList<StreetAddressUpsert>>(l => l.Count == 1 && l[0].GooglePlaceId == "good"),
            Arg.Any<CancellationToken>());
        await _addressResultRepo.Received(1).DeleteByPlaceIdsAsync(
            Arg.Is<IReadOnlyList<string>>(l => l.Count == 0), Arg.Any<CancellationToken>());
        Assert.Equal(new RefreshStaleResultTuple(Examined: 2, Refreshed: 1, Deleted: 0, Failed: 1, Skipped: 0), Tuple(result));
    }

    // Compare summaries by value without coupling tests to a specific record type layout.
    private sealed record RefreshStaleResultTuple(int Examined, int Refreshed, int Deleted, int Failed, int Skipped);

    private static RefreshStaleResultTuple Tuple(RefreshStaleResult r) =>
        new(r.Examined, r.Refreshed, r.Deleted, r.Failed, r.Skipped);
}
