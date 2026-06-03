using FastAddress.Api.Database.Entities;
using FastAddress.Api.Database.Models;
using FastAddress.Api.Database.Options;
using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Services;
using FastAddress.Api.Services.Messages;
using FastAddress.Api.Services.Messages.Events;
using FastAddress.Api.Services.Models;
using FastAddress.Api.Tests.Messages;
using FastAddress.Api.Vendors.Contracts;
using FastAddress.Api.Vendors.Models;
using FastAddress.TestUtilities;

using Microsoft.Extensions.Options;

using NetTopologySuite.Geometries;

using NodaTime;
using NodaTime.Testing;

using NSubstitute;

namespace FastAddress.Api.Tests.Services;

public sealed class AddressSearchServiceTests
{
    private static readonly Instant Now = Instant.FromUtc(2026, 6, 3, 12, 0, 0);
    private static readonly Duration MaxReuseAge = Duration.FromDays(90);

    private readonly IAddressQueryRepository _queryRepo = Substitute.For<IAddressQueryRepository>();
    private readonly IAddressResultRepository _addressRepo = Substitute.For<IAddressResultRepository>();
    private readonly IGooglePlacesService _placesService = Substitute.For<IGooglePlacesService>();
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();
    private readonly IOptionsMonitor<AddressSearchOptions> _options = Substitute.For<IOptionsMonitor<AddressSearchOptions>>();
    private readonly FakeClock _clock = new(Now);

    private readonly AddressSearchService _addressSearchService;

    public AddressSearchServiceTests()
    {
        _options.CurrentValue.Returns(new AddressSearchOptions { MaxReuseAge = MaxReuseAge });
        _addressSearchService = new AddressSearchService(
            _queryRepo, _addressRepo, _placesService, _eventPublisher, _options, _clock);
    }

    private static SearchStreetAddressQuery Query(string text, int limit = 5) =>
        new() { Text = text, Limit = limit, LocationBias = null };

    // Every field a test asserts on is a parameter, so the expectation is visible in the test itself.
    private static StreetAddressMatch Match(
        string placeId = "place-x",
        double similarity = 0.5,
        string streetLine = "Some street",
        string? postalCode = null,
        string? postalTown = null) =>
        new()
        {
            StreetAddress = new StreetAddressResult
            {
                GooglePlaceId = placeId,
                StreetLine = streetLine,
                PostalCode = postalCode,
                PostalTown = postalTown,
                Location = GeoTestData.Point(10.0, 63.0),
            },
            Similarity = similarity,
        };

    // Ledger refreshed exactly now, inside the reuse window, so it counts as fresh.
    private void MockFreshLedger(string normalizedQuery) =>
        _queryRepo.FindByQueryAsync(normalizedQuery, Arg.Any<CancellationToken>())
            .Returns(new StreetAddressQuery { Query = normalizedQuery, LastRefreshed = Now });

    // No ledger row at all, so the query has never been fetched and counts as stale.
    private void MockMissingLedger(string normalizedQuery) =>
        _queryRepo.FindByQueryAsync(normalizedQuery, Arg.Any<CancellationToken>())
            .Returns((StreetAddressQuery?)null);

    // Ledger refreshed one day past the reuse window, so it counts as stale.
    private void MockStaleLedger(string normalizedQuery) =>
        _queryRepo.FindByQueryAsync(normalizedQuery, Arg.Any<CancellationToken>())
            .Returns(new StreetAddressQuery
            {
                Query = normalizedQuery,
                LastRefreshed = Now - MaxReuseAge - Duration.FromDays(1),
            });

    private void MockDbResults(params StreetAddressMatch[] matches) =>
        _addressRepo.Search(Arg.Any<string>(), Arg.Any<Point?>(), Arg.Any<int>())
            .Returns(matches.AsAsyncEnumerable());

    private void MockGoogleResults(params AddressSearchResult[] results) =>
        _placesService.Search(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(results.AsAsyncEnumerable());

    private static async IAsyncEnumerable<AddressSearchResult> ThrowAfter(AddressSearchResult first)
    {
        yield return first;
        await Task.CompletedTask;
        // Simulates the request token firing mid-stream after the first result was served.
        throw new OperationCanceledException();
    }

    [Fact]
    public async Task Search_FreshLedger_StreamsDatabaseMatches()
    {
        // Arrange. The ledger is fresh, so the cache is authoritative.
        MockFreshLedger(normalizedQuery: "LADE ALLE");
        MockDbResults(
            Match(placeId: "place-0", similarity: 0.9, streetLine: "Lade alle 77"),
            Match(placeId: "place-1", similarity: 0.6, streetLine: "Lade alle 79"));

        // Act
        var results = await _addressSearchService.Search(Query("Lade alle")).CollectAsync();

        // Assert - Each entry is projected straight from its database row. Score is the trigram similarity.
        Assert.Equal(new[] { "place-0", "place-1" }, results.Select(r => r.PlaceId));
        Assert.Equal(new[] { "Lade alle 77", "Lade alle 79" }, results.Select(r => r.StreetLine));
        Assert.Equal(0.9, results[0].Score);
    }

    [Fact]
    public async Task Search_FreshLedger_ProjectsPostalCodeAndTown()
    {
        // Arrange
        MockFreshLedger(normalizedQuery: "LADE ALLE");
        MockDbResults(Match(postalCode: "7041", postalTown: "Trondheim"));

        // Act
        var entry = Assert.Single(await _addressSearchService.Search(Query("Lade alle")).CollectAsync());

        // Assert
        Assert.Equal("7041", entry.PostalCode);
        Assert.Equal("Trondheim", entry.PostalTown);
    }

    [Fact]
    public async Task Search_FreshLedger_DoesNotCallGoogleOrPublish()
    {
        // Arrange
        MockFreshLedger(normalizedQuery: "LADE ALLE");
        MockDbResults(Match(placeId: "place-0"));

        // Act
        await _addressSearchService.Search(Query("Lade alle")).CollectAsync();

        // Assert - A fresh ledger means Google is never consulted and nothing is published.
        _placesService.DidNotReceive().Search(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>());
        _eventPublisher.DidNotReceive().TryPublish(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public async Task Search_MissingLedger_FetchesGoogleAndStreamsEntriesWithNullScore()
    {
        // Arrange. A never-searched query goes straight to Google.
        MockMissingLedger(normalizedQuery: "LADE ALLE");
        MockGoogleResults(
            EventTestBuilders.GoogleResult("street-0"),
            EventTestBuilders.GoogleResult("street-1"));

        // Act
        var results = await _addressSearchService.Search(Query("Lade alle")).CollectAsync();

        // Assert - Entries come from Google and score is null because no similarity was computed.
        Assert.Equal(new[] { "street-0", "street-1" }, results.Select(r => r.PlaceId));
        Assert.All(results, r => Assert.Null(r.Score));
        _addressRepo.DidNotReceive().Search(Arg.Any<string>(), Arg.Any<Point?>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Search_StaleLedger_FetchesGoogle()
    {
        // Arrange. A ledger past the reuse window is treated like a miss.
        MockStaleLedger(normalizedQuery: "LADE ALLE");
        MockGoogleResults(EventTestBuilders.GoogleResult("street-0"));

        // Act
        var results = await _addressSearchService.Search(Query("Lade alle")).CollectAsync();

        // Assert
        var entry = Assert.Single(results);
        Assert.Equal("street-0", entry.PlaceId);
    }

    [Fact]
    public async Task Search_StaleLedger_PublishesRetrievedResultsAfterStreamDrains()
    {
        // Arrange
        MockMissingLedger(normalizedQuery: "LADE ALLE");
        MockGoogleResults(EventTestBuilders.GoogleResult("street-1"));

        // Act
        await _addressSearchService.Search(Query("Lade alle")).CollectAsync();

        // Assert - The persistence handler is handed the normalized query and the upserts.
        _eventPublisher.Received(1).TryPublish(Arg.Is<GoogleResultsRetrieved>(
            e => e.NormalizedQuery == "LADE ALLE"
                && e.Results.Count == 1
                && e.Results[0].GooglePlaceId == "street-1"));
    }

    [Fact]
    public async Task Search_StaleLedger_DropsNonStreetResults()
    {
        // Arrange. Google returns a bare locality alongside a street address. Only the latter survives.
        MockMissingLedger(normalizedQuery: "LADE ALLE");
        MockGoogleResults(
            EventTestBuilders.GoogleResult("locality-0", type: "locality"),
            EventTestBuilders.GoogleResult("street-1", type: "street_address"));

        // Act
        var results = await _addressSearchService.Search(Query("Lade alle")).CollectAsync();

        // Assert - Non-street results are excluded from both the stream and the published upserts.
        var entry = Assert.Single(results);
        Assert.Equal("street-1", entry.PlaceId);
        _eventPublisher.Received(1).TryPublish(Arg.Is<GoogleResultsRetrieved>(
            e => e.Results.Count == 1 && e.Results[0].GooglePlaceId == "street-1"));
    }

    [Fact]
    public async Task Search_StaleLedger_EmptyGoogleResult_PublishesEmptyToMarkFresh()
    {
        // Arrange. Google found nothing, but the ledger must still be marked fresh.
        MockMissingLedger(normalizedQuery: "LADE ALLE");
        MockGoogleResults();

        // Act
        var results = await _addressSearchService.Search(Query("Lade alle")).CollectAsync();

        // Assert
        Assert.Empty(results);
        _eventPublisher.Received(1).TryPublish(Arg.Is<GoogleResultsRetrieved>(e => e.Results.Count == 0));
    }

    [Fact]
    public async Task Search_StaleLedger_ForwardsQueryAndBiasToGoogle()
    {
        // Arrange. The query and bias the user searched must reach the vendor request unchanged.
        var bias = GeoTestData.Point(10.4, 63.4);
        MockMissingLedger(normalizedQuery: "LADE ALLE");
        MockGoogleResults(EventTestBuilders.GoogleResult("street-1"));
        var query = new SearchStreetAddressQuery { Text = "Lade alle", Limit = 5, LocationBias = bias };

        // Act
        await _addressSearchService.Search(query).CollectAsync();

        // Assert
        _placesService.Received(1).Search(
            Arg.Is<AddressSearchRequest>(r => r.Query == "Lade alle" && r.LocationBias == bias),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_StaleLedger_StreamFaultsMidway_DoesNotPublish()
    {
        // Arrange. The Google stream throws after the first result, as a cancelled request would.
        MockMissingLedger(normalizedQuery: "LADE ALLE");
        _placesService.Search(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(ThrowAfter(EventTestBuilders.GoogleResult("street-0")));

        // Act + Assert. The fault propagates and the publish after the loop never runs, so a partial
        // fetch cannot mark the ledger fresh.
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in _addressSearchService.Search(Query("Lade alle")))
            {
            }
        });

        _eventPublisher.DidNotReceive().TryPublish(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public async Task Search_StaleLedger_CallerStopsEarly_DoesNotPublish()
    {
        // Arrange
        MockMissingLedger(normalizedQuery: "LADE ALLE");
        MockGoogleResults(
            EventTestBuilders.GoogleResult("street-0"),
            EventTestBuilders.GoogleResult("street-1"));

        // Act - Consume only the first entry, then abandon the stream.
        await foreach (var _ in _addressSearchService.Search(Query("Lade alle")))
        {
            break;
        }

        // Assert - Unlike the old publish-in-finally, abandoning the stream persists nothing.
        _eventPublisher.DidNotReceive().TryPublish(Arg.Any<IDomainEvent>());
    }
}
