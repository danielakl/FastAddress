using FastAddress.Api.Database.Entities;
using FastAddress.Api.Database.Models;
using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Services;
using FastAddress.Api.Services.Messages;
using FastAddress.Api.Services.Messages.Events;
using FastAddress.Api.Services.Models;
using FastAddress.TestUtilities;

using NetTopologySuite.Geometries;

using NSubstitute;

namespace FastAddress.Api.Tests.Services;

public sealed class StreetAddressSearchServiceTests
{
    private readonly IStreetAddressRepository _streetAddressRepo = Substitute.For<IStreetAddressRepository>();
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();

    private readonly StreetAddressSearchService _streetAddressSearchService;

    public StreetAddressSearchServiceTests() =>
        _streetAddressSearchService = new StreetAddressSearchService(_streetAddressRepo, _eventPublisher);

    private static SearchStreetAddressQuery Query(int limit = 5) =>
        new() { Text = "Lade alle 77", Limit = limit, LocationBias = null };

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

    private void MockSearchResults(params StreetAddressMatch[] matches) =>
        _streetAddressRepo.Search(Arg.Any<string>(), Arg.Any<Point?>(), Arg.Any<int>())
            .Returns(matches.AsAsyncEnumerable());

    [Fact]
    public async Task Search_StreamsCacheMatchesAsEntries()
    {
        // Arrange
        MockSearchResults(
            Match(placeId: "place-0", similarity: 0.9, streetLine: "Lade alle 77"),
            Match(placeId: "place-1", similarity: 0.6, streetLine: "Lade alle 79"));

        // Act
        var results = await _streetAddressSearchService.Search(Query()).CollectAsync();

        // Assert - each entry is projected straight from its database row.
        Assert.Equal(new[] { "place-0", "place-1" }, results.Select(r => r.PlaceId));
        Assert.Equal(new[] { "Lade alle 77", "Lade alle 79" }, results.Select(r => r.StreetLine));
        Assert.Equal(0.9, results[0].Score);
    }

    [Fact]
    public async Task Search_ProjectsPostalCodeAndTown()
    {
        // Arrange
        MockSearchResults(Match(postalCode: "7041", postalTown: "Trondheim"));

        // Act
        var entry = Assert.Single(await _streetAddressSearchService.Search(Query()).CollectAsync());

        // Assert
        Assert.Equal("7041", entry.PostalCode);
        Assert.Equal("Trondheim", entry.PostalTown);
    }

    [Fact]
    public async Task Search_PublishesEventWithStreamedMatchCount()
    {
        // Arrange - two cached rows for this query.
        var query = Query(limit: 5);
        MockSearchResults(Match(placeId: "place-0"), Match(placeId: "place-1"));

        // Act
        await _streetAddressSearchService.Search(query).CollectAsync();

        // Assert - the background worker is handed the query and the count that streamed.
        _eventPublisher.Received(1).TryPublish(
            Arg.Is<AddressSearchPerformed>(e => e.MatchCount == 2 && e.Query == query));
    }

    [Fact]
    public async Task Search_NoMatches_PublishesEventWithZeroCount()
    {
        // Arrange - a cold cache yields nothing but still triggers a background fetch.
        MockSearchResults();

        // Act
        var results = await _streetAddressSearchService.Search(Query()).CollectAsync();

        // Assert
        Assert.Empty(results);
        _eventPublisher.Received(1).TryPublish(Arg.Is<AddressSearchPerformed>(e => e.MatchCount == 0));
    }

    [Fact]
    public async Task Search_CallerStopsEarly_StillPublishesEvent()
    {
        // Arrange
        MockSearchResults(Match(placeId: "place-0"), Match(placeId: "place-1"));

        // Act - consume only the first entry, then abandon the stream.
        await foreach (var _ in _streetAddressSearchService.Search(Query()))
        {
            break;
        }

        // Assert - the finally publishes even though enumeration stopped early.
        _eventPublisher.Received(1).TryPublish(Arg.Any<AddressSearchPerformed>());
    }
}
