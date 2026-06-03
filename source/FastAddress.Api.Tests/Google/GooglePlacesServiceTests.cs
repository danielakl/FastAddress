using System.Net;

using FastAddress.Api.Vendors.Google.Places;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;
using FastAddress.Sdk.Exceptions;
using FastAddress.TestUtilities;

using NSubstitute;

using Refit;

namespace FastAddress.Api.Tests.Google;

public sealed class GooglePlacesServiceTests
{
    private readonly IPlacesApi places = Substitute.For<IPlacesApi>();

    private GooglePlacesService CreateService() => new(places);

    private void GivenAutocomplete(params PlacePrediction?[] predictions) =>
        places.AutocompleteAsync(
                Arg.Any<AutocompleteRequest>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(PlacesBuilders.Autocomplete(predictions));

    private void GivenPlace(PlaceDetailsResponse details) =>
        places.GetPlaceAsync(
                details.Id,
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(details);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_BlankQuery_YieldsNothingAndCallsNoApi(string query)
    {
        // Arrange
        var service = CreateService();

        // Act
        var results = await service.Search(new AddressSearchRequest { Query = query, Limit = null })
            .CollectAsync();

        // Assert
        Assert.Empty(results);
        await places.DidNotReceiveWithAnyArgs().AutocompleteAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Search_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.Search(null!).CollectAsync());
    }

    [Fact]
    public async Task Search_LimitProvided_FetchesOnlyTheRequestedNumberOfPredictions()
    {
        // Arrange
        GivenAutocomplete(PlacesBuilders.Prediction("p0"), PlacesBuilders.Prediction("p1"), PlacesBuilders.Prediction("p2"));
        GivenPlace(PlacesBuilders.Place("p0"));
        GivenPlace(PlacesBuilders.Place("p1"));
        GivenPlace(PlacesBuilders.Place("p2"));
        var service = CreateService();

        // Act
        var results = await service.Search(new AddressSearchRequest { Query = "lade", Limit = 2 })
            .CollectAsync();

        // Assert
        Assert.Equal(2, results.Count);
        await places.DidNotReceive().GetPlaceAsync("p2", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_SuggestionWithNullPrediction_IsSkipped()
    {
        // Arrange
        GivenAutocomplete(PlacesBuilders.Prediction("p0"), null, PlacesBuilders.Prediction("p1"));
        GivenPlace(PlacesBuilders.Place("p0"));
        GivenPlace(PlacesBuilders.Place("p1"));
        var service = CreateService();

        // Act
        var results = await service.Search(new AddressSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Search_PlaceMissingLocation_IsSkipped()
    {
        // Arrange
        GivenAutocomplete(PlacesBuilders.Prediction("p0"), PlacesBuilders.Prediction("p1"));
        GivenPlace(PlacesBuilders.Place("p0", "Addr 0"));
        GivenPlace(PlacesBuilders.Place("p1", longitude: null, latitude: null));
        var service = CreateService();

        // Act
        var results = await service.Search(new AddressSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        Assert.Equal("p0", Assert.Single(results).PlaceId);
    }

    [Fact]
    public async Task Search_PlaceMissingShortFormattedAddress_IsSkipped()
    {
        // Arrange
        GivenAutocomplete(PlacesBuilders.Prediction("p0"), PlacesBuilders.Prediction("p1"));
        GivenPlace(PlacesBuilders.Place("p0", "Addr 0"));
        GivenPlace(PlacesBuilders.Place("p1", shortAddress: null));
        var service = CreateService();

        // Act
        var results = await service.Search(new AddressSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        Assert.Equal("p0", Assert.Single(results).PlaceId);
    }

    [Fact]
    public async Task Search_PlaceHasMovedPlaceId_UsesMovedPlaceIdAsResultPlaceId()
    {
        // Arrange
        GivenAutocomplete(PlacesBuilders.Prediction("p0"));
        GivenPlace(PlacesBuilders.Place("p0", movedPlaceId: "moved-1"));
        var service = CreateService();

        // Act
        var results = await service.Search(new AddressSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        Assert.Equal("moved-1", Assert.Single(results).PlaceId);
    }

    [Fact]
    public async Task Search_WithLocationBias_SendsCircleAroundPointToAutocomplete()
    {
        // Arrange
        GivenAutocomplete(PlacesBuilders.Prediction("p0"));
        GivenPlace(PlacesBuilders.Place("p0"));
        var bias = GeoTestData.Point(10.4, 63.4);
        var service = CreateService();

        // Act
        await service.Search(new AddressSearchRequest { Query = "lade", Limit = null, LocationBias = bias })
            .CollectAsync();

        // Assert - Point Y/X map to latitude/longitude, fixed 25km radius.
        await places.Received(1).AutocompleteAsync(
            Arg.Is<AutocompleteRequest>(r => r.LocationBias != null
                && r.LocationBias.Circle.Center.Latitude == 63.4
                && r.LocationBias.Circle.Center.Longitude == 10.4
                && r.LocationBias.Circle.Radius == 25_000),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_WithoutLocationBias_SendsNoLocationBias()
    {
        // Arrange
        GivenAutocomplete(PlacesBuilders.Prediction("p0"));
        GivenPlace(PlacesBuilders.Place("p0"));
        var service = CreateService();

        // Act
        await service.Search(new AddressSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        await places.Received(1).AutocompleteAsync(
            Arg.Is<AutocompleteRequest>(r => r.LocationBias == null),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_AutocompleteReturnsNoPredictions_YieldsNothing()
    {
        // Arrange
        GivenAutocomplete();
        var service = CreateService();

        // Act
        var results = await service.Search(new AddressSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        Assert.Empty(results);
    }

    // Build the Refit error Google's client throws on a non-success status (for example a 429 quota hit).
    private static Task<ApiException> ApiError(HttpStatusCode status)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://places.googleapis.com");
        using var response = new HttpResponseMessage(status);
        return ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    [Fact]
    public async Task Search_AutocompleteFailsWithStatus_ThrowsProblemDetailsExceptionCarryingThatStatus()
    {
        // Arrange. Google rejects the autocomplete request because the daily quota is exhausted (429).
        var apiError = await ApiError(HttpStatusCode.TooManyRequests);
        places.AutocompleteAsync(
                Arg.Any<AutocompleteRequest>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AutocompleteResponse>(apiError));
        var service = CreateService();

        // Act + Assert. The transport error is translated to a problem-details exception with the status.
        var thrown = await Assert.ThrowsAsync<ProblemDetailsException>(async () =>
            await service.Search(new AddressSearchRequest { Query = "lade", Limit = null }).CollectAsync());
        Assert.Equal(429, thrown.StatusCode);
        Assert.Same(apiError, thrown.InnerException);
    }

    [Fact]
    public async Task Search_PlaceDetailsFailsWithStatus_ThrowsProblemDetailsExceptionCarryingThatStatus()
    {
        // Arrange. Autocomplete succeeds, but fetching the place details hits the quota limit.
        GivenAutocomplete(PlacesBuilders.Prediction("p0"));
        var apiError = await ApiError(HttpStatusCode.TooManyRequests);
        places.GetPlaceAsync("p0", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PlaceDetailsResponse>(apiError));
        var service = CreateService();

        // Act + Assert
        var thrown = await Assert.ThrowsAsync<ProblemDetailsException>(async () =>
            await service.Search(new AddressSearchRequest { Query = "lade", Limit = null }).CollectAsync());
        Assert.Equal(429, thrown.StatusCode);
    }
}
