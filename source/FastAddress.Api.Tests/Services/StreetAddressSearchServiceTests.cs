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
                PostalCode = "7041",
                PostalTown = "Trondheim",
                Location = GeoTestData.Point(10.0, 63.0),
            },
            Similarity = similarity,
        };

    private static AddressSearchResult GoogleResult(string placeId, int orderScore, IReadOnlyList<string>? types = null) =>
        new()
        {
            PlaceId = placeId,
            OrderScore = orderScore,
            ShortFormattedAddress = "Lade alle 77 a, Trondheim",
            Location = GeoTestData.Point(10.46, 63.44),
            Types = types ?? ["street_address"],
            AddressComponents =
            [
                new AddressComponent { LongText = "Lade alle", ShortText = "Lade alle", Types = [AddressComponentTypes.Route] },
                new AddressComponent { LongText = "77 a", ShortText = "77 a", Types = [AddressComponentTypes.StreetNumber] },
                new AddressComponent { LongText = "7041", ShortText = "7041", Types = [AddressComponentTypes.PostalCode] },
                new AddressComponent { LongText = "Trondheim", ShortText = "Trondheim", Types = [AddressComponentTypes.PostalTown] },
            ],
        };

    private void GivenCacheReturns(params StreetAddressMatch[] matches) =>
        repository.Search(Arg.Any<string>(), Arg.Any<Point?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(matches.AsAsyncEnumerable());

    private void GivenGoogleReturns(params AddressSearchResult[] results) =>
        places.Search(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(results.AsAsyncEnumerable());

    [Fact]
    public async Task Search_ConfidentCacheHit_DoesNotCallGoogle()
    {
        // Arrange — a single near-exact match clears the short-circuit threshold.
        GivenCacheReturns(Match(0.95));

        // Act
        var results = await service.Search(Query()).CollectAsync();

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsCacheHit);
        places.DidNotReceive().Search(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_SingleMatchAboveConfidence_ServesFromCacheRegardlessOfCount()
    {
        // Arrange — one match at 0.6 clears the 0.5 confidence bar even though only a single row exists
        // (the removed result-count gate would previously have forced a Google call here).
        GivenCacheReturns(Match(0.6));

        // Act
        var results = await service.Search(Query()).CollectAsync();

        // Assert
        Assert.True(Assert.Single(results).IsCacheHit);
        places.DidNotReceive().Search(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_CacheMiss_DropsNonStreetAddressResults()
    {
        // Arrange — Google returns a bare locality alongside a street address; only the latter is usable.
        GivenCacheReturns();
        GivenGoogleReturns(
            GoogleResult("locality-0", orderScore: 0, types: ["locality"]),
            GoogleResult("street-1", orderScore: 1));

        // Act
        var results = await service.Search(Query()).CollectAsync();

        // Assert
        Assert.Equal("street-1", Assert.Single(results).PlaceId);
        await repository.DidNotReceive().UpsertByPlaceIdAsync(
            "locality-0", Arg.Any<StreetAddressUpsert>(), Arg.Any<CancellationToken>());
        await repository.Received(1).UpsertByPlaceIdAsync(
            "street-1", Arg.Any<StreetAddressUpsert>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_CacheMiss_CallsGoogleAndUpsertsEachResult()
    {
        // Arrange — no cached matches forces the Google fallback.
        GivenCacheReturns();
        GivenGoogleReturns(GoogleResult("place-0", 0), GoogleResult("place-1", 1));

        // Act
        var results = await service.Search(Query()).CollectAsync();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.False(r.IsCacheHit));
        await repository.Received(2).UpsertByPlaceIdAsync(
            Arg.Any<string>(), Arg.Any<StreetAddressUpsert>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_CacheHit_ProjectsPostalCodeAndTown()
    {
        // Arrange — a confident cached row carries postal data through to the entry.
        GivenCacheReturns(Match(0.95));

        // Act
        var entry = Assert.Single(await service.Search(Query()).CollectAsync());

        // Assert
        Assert.Equal("7041", entry.PostalCode);
        Assert.Equal("Trondheim", entry.PostalTown);
    }

    [Fact]
    public async Task Search_GoogleResult_ProjectsPostalCodeAndTownFromComponents()
    {
        // Arrange — cache miss; postal components from Google flow into the entry.
        GivenCacheReturns();
        GivenGoogleReturns(GoogleResult("place-0", 0));

        // Act
        var entry = Assert.Single(await service.Search(Query()).CollectAsync());

        // Assert
        Assert.Equal("7041", entry.PostalCode);
        Assert.Equal("Trondheim", entry.PostalTown);
    }

    [Fact]
    public async Task Search_CacheMiss_ForwardsLocationBiasToGoogle()
    {
        // Arrange — a bias point on the query must reach the vendor request, not be dropped.
        var bias = GeoTestData.Point(10.4, 63.4);
        var query = new SearchStreetAddressQuery { Text = "Lade alle 77", Limit = 5, LocationBias = bias };
        GivenCacheReturns();
        GivenGoogleReturns(GoogleResult("place-0", 0));

        // Act
        await service.Search(query).CollectAsync();

        // Assert
        places.Received(1).Search(
            Arg.Is<AddressSearchRequest>(r => r.LocationBias == bias),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_CacheReadThrows_FallsThroughToGoogle()
    {
        // Arrange — the DB read fails; the service should degrade to Google rather than throw.
        repository.Search(Arg.Any<string>(), Arg.Any<Point?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("db down"));
        GivenGoogleReturns(GoogleResult("place-0", 0));

        // Act
        var results = await service.Search(Query()).CollectAsync();

        // Assert
        Assert.Single(results);
        places.Received(1).Search(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>());
    }
}
