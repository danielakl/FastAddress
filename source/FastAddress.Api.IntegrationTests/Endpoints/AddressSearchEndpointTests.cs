using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

using FastAddress.Api.Database.Entities;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;
using FastAddress.Sdk.Exceptions;
using FastAddress.TestUtilities;

using NodaTime;

using NSubstitute;

namespace FastAddress.Api.IntegrationTests.Endpoints;

public sealed class AddressSearchEndpointTests : IClassFixture<ApiFactory>
{
    private const string SearchRoute = "/addresses/search";

    private readonly ApiFactory _factory;

    public AddressSearchEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        // The Google mock is shared across the fixture, so reset its recorded calls before each test.
        _factory.GooglePlacesMock.ClearReceivedCalls();
    }

    [Fact]
    public async Task Search_MissingLedger_FetchesFromGoogleAndReturnsResults()
    {
        // Arrange - Nothing cached for this query, so the service must call Google.
        _factory.GooglePlacesMock
            .SearchPlaces(Arg.Any<GooglePlacesSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new[] { StreetPlace("g-1", "Storgata", "1") }.AsAsyncEnumerable());
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(SearchRoute, new { address = "Storgata", limit = 5 });
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Storgata 1", body);
        _factory.GooglePlacesMock.Received(1)
            .SearchPlaces(Arg.Any<GooglePlacesSearchRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_FreshLedger_ServesDatabaseCacheWithoutCallingGoogle()
    {
        // Arrange - Seed a fresh ledger entry and a matching result so the cache is authoritative.
        var now = SystemClock.Instance.GetCurrentInstant();
        await _factory.WithDbContextAsync(async db =>
        {
            db.StreetAddressQueries.Add(new StreetAddressQuery { Query = "LADE ALLE", LastRefreshed = now });
            db.StreetAddressResults.Add(new StreetAddressResult
            {
                GooglePlaceId = "seed-1",
                StreetLine = "Lade alle 77",
                SearchText = "LADE ALLE 77",
                PostalCode = "7041",
                PostalTown = "Trondheim",
                Location = GeoTestData.Point(10.0, 63.0),
                LastRefreshed = now,
            });
            await db.SaveChangesAsync();
        });
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(SearchRoute, new { address = "Lade alle", limit = 5 });
        var body = await response.Content.ReadAsStringAsync();

        // Assert - The real trigram query returns the seeded row and Google is never consulted.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Lade alle 77", body);
        _factory.GooglePlacesMock.DidNotReceive()
            .SearchPlaces(Arg.Any<GooglePlacesSearchRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_GoogleFails_ReturnsProblemDetailsWithUpstreamStatus()
    {
        // Arrange - A never-cached query reaches Google, which reports an exhausted quota.
        _factory.GooglePlacesMock
            .SearchPlaces(Arg.Any<GooglePlacesSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(QuotaExceeded());
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(SearchRoute, new { address = "Kongens gate", limit = 5 });

        // Assert - The vendor failure surfaces as an RFC 7807 problem-details response carrying the status.
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    private static GooglePlace StreetPlace(string placeId, string street, string number) =>
        new()
        {
            PlaceId = placeId,
            ShortFormattedAddress = $"{street} {number}",
            Location = GeoTestData.Point(10.0, 63.0),
            Types = ["street_address"],
            AddressComponents =
            [
                new AddressComponent { LongText = street, ShortText = street, Types = [AddressComponentTypes.Route] },
                new AddressComponent { LongText = number, ShortText = number, Types = [AddressComponentTypes.StreetNumber] },
            ],
        };

    // An async stream that faults before yielding, as the Places service does on an upstream error.
    [DoesNotReturn]
    private static async IAsyncEnumerable<GooglePlace> QuotaExceeded()
    {
        await Task.CompletedTask;
        throw new ProblemDetailsException(statusCode: (int)HttpStatusCode.TooManyRequests, title: "Address search failed");
#pragma warning disable CS0162 // Unreachable code detected
        yield break;
#pragma warning restore CS0162 // Unreachable code detected
    }
}
