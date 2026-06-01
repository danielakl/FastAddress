using FastAddress.Api.Database.Entities;
using FastAddress.Api.Database.Repositories;
using FastAddress.Api.Models;
using FastAddress.Api.Options;
using FastAddress.Api.Services;
using FastAddress.Api.Vendors.Contracts;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;
using FastAddress.TestUtilities;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NetTopologySuite.Geometries;

using NSubstitute;

namespace FastAddress.Api.Tests.Services;

public sealed class StreetAddressSearchServiceTests
{
    private readonly IStreetAddressRepository repository = Substitute.For<IStreetAddressRepository>();
    private readonly IGooglePlacesService places = Substitute.For<IGooglePlacesService>();
    private readonly IOptionsMonitor<AddressSearchOptions> options = Substitute.For<IOptionsMonitor<AddressSearchOptions>>();

    private readonly StreetAddressSearchService service;

    public StreetAddressSearchServiceTests()
    {
        options.CurrentValue.Returns(new AddressSearchOptions());
        service = new StreetAddressSearchService(
            repository, places, options, NullLogger<StreetAddressSearchService>.Instance);
    }

    private static SearchStreetAddressQuery Query() =>
        new() { Text = "Lade alle 77", Limit = 5, LocationBias = null };

    private static StreetAddressMatch Match(double similarity) =>
        new()
        {
            StreetAddress = new StreetAddress
            {
                GooglePlaceId = "place-cache",
                StreetLine = "Lade alle 77",
                Location = GeoTestData.Point(10.0, 63.0),
            },
            Similarity = similarity,
        };

    private static AddressSearchResult GoogleResult(string placeId, int orderScore) =>
        new()
        {
            PlaceId = placeId,
            OrderScore = orderScore,
            ShortFormattedAddress = "Lade alle 77 a, Trondheim",
            Location = GeoTestData.Point(10.46, 63.44),
            Types = ["street_address"],
            AddressComponents =
            [
                new AddressComponent { LongText = "Lade alle", ShortText = "Lade alle", Types = [AddressComponentTypes.Route] },
                new AddressComponent { LongText = "77 a", ShortText = "77 a", Types = [AddressComponentTypes.StreetNumber] },
            ],
        };

    private void GivenCacheReturns(params StreetAddressMatch[] matches) =>
        repository.SearchAsync(Arg.Any<string>(), Arg.Any<Point?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(matches.AsAsyncEnumerable());

    private void GivenGoogleReturns(params AddressSearchResult[] results) =>
        places.SearchAsync(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(results.AsAsyncEnumerable());

    [Fact]
    public async Task SearchAsync_ConfidentCacheHit_DoesNotCallGoogle()
    {
        // Arrange — a single near-exact match clears the short-circuit threshold.
        GivenCacheReturns(Match(0.95));

        // Act
        var results = await service.SearchAsync(Query()).CollectAsync();

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsCacheHit);
        places.DidNotReceive().SearchAsync(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_CacheMiss_CallsGoogleAndUpsertsEachResult()
    {
        // Arrange — no cached matches forces the Google fallback.
        GivenCacheReturns();
        GivenGoogleReturns(GoogleResult("place-0", 0), GoogleResult("place-1", 1));

        // Act
        var results = await service.SearchAsync(Query()).CollectAsync();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.False(r.IsCacheHit));
        await repository.Received(2).UpsertByPlaceIdAsync(
            Arg.Any<string>(), Arg.Any<StreetAddressUpsert>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_CacheReadThrows_FallsThroughToGoogle()
    {
        // Arrange — the DB read fails; the service should degrade to Google rather than throw.
        repository.SearchAsync(Arg.Any<string>(), Arg.Any<Point?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("db down"));
        GivenGoogleReturns(GoogleResult("place-0", 0));

        // Act
        var results = await service.SearchAsync(Query()).CollectAsync();

        // Assert
        Assert.Single(results);
        places.Received(1).SearchAsync(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>());
    }
}
