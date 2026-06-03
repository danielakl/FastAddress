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
    private readonly IPlacesApi _placesApi = Substitute.For<IPlacesApi>();

    private GooglePlacesService CreateService() => new(_placesApi);

    private void MockAutocomplete(params PlacePrediction?[] predictions) =>
        _placesApi.AutocompleteAsync(
                Arg.Any<AutocompleteRequest>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(PlacesBuilders.Autocomplete(predictions));

    private void MockGetPlace(PlaceDetailsResponse details) =>
        _placesApi.GetPlaceAsync(
                details.Id,
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(details);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchPlaces_BlankQuery_YieldsNothingAndCallsNoApi(string query)
    {
        // Arrange
        var service = CreateService();

        // Act
        var results = await service.SearchPlaces(new GooglePlacesSearchRequest { Query = query, Limit = null })
            .CollectAsync();

        // Assert
        Assert.Empty(results);
        await _placesApi.DidNotReceiveWithAnyArgs().AutocompleteAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task SearchPlaces_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.SearchPlaces(null!).CollectAsync());
    }

    [Fact]
    public async Task SearchPlaces_LimitProvided_FetchesOnlyTheRequestedNumberOfPredictions()
    {
        // Arrange
        MockAutocomplete(PlacesBuilders.Prediction("p0"), PlacesBuilders.Prediction("p1"), PlacesBuilders.Prediction("p2"));
        MockGetPlace(PlacesBuilders.Place("p0"));
        MockGetPlace(PlacesBuilders.Place("p1"));
        MockGetPlace(PlacesBuilders.Place("p2"));
        var service = CreateService();

        // Act
        var results = await service.SearchPlaces(new GooglePlacesSearchRequest { Query = "lade", Limit = 2 })
            .CollectAsync();

        // Assert
        Assert.Equal(2, results.Count);
        await _placesApi.DidNotReceive().GetPlaceAsync("p2", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchPlaces_SuggestionWithNullPrediction_IsSkipped()
    {
        // Arrange
        MockAutocomplete(PlacesBuilders.Prediction("p0"), null, PlacesBuilders.Prediction("p1"));
        MockGetPlace(PlacesBuilders.Place("p0"));
        MockGetPlace(PlacesBuilders.Place("p1"));
        var service = CreateService();

        // Act
        var results = await service.SearchPlaces(new GooglePlacesSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task SearchPlaces_PlaceMissingLocation_IsSkipped()
    {
        // Arrange
        MockAutocomplete(PlacesBuilders.Prediction("p0"), PlacesBuilders.Prediction("p1"));
        MockGetPlace(PlacesBuilders.Place("p0", "Addr 0"));
        MockGetPlace(PlacesBuilders.Place("p1", longitude: null, latitude: null));
        var service = CreateService();

        // Act
        var results = await service.SearchPlaces(new GooglePlacesSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        Assert.Equal("p0", Assert.Single(results).PlaceId);
    }

    [Fact]
    public async Task SearchPlaces_PlaceMissingShortFormattedAddress_IsSkipped()
    {
        // Arrange
        MockAutocomplete(PlacesBuilders.Prediction("p0"), PlacesBuilders.Prediction("p1"));
        MockGetPlace(PlacesBuilders.Place("p0", "Addr 0"));
        MockGetPlace(PlacesBuilders.Place("p1", shortAddress: null));
        var service = CreateService();

        // Act
        var results = await service.SearchPlaces(new GooglePlacesSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        Assert.Equal("p0", Assert.Single(results).PlaceId);
    }

    [Fact]
    public async Task SearchPlaces_PlaceHasMovedPlaceId_UsesMovedPlaceIdAsResultPlaceId()
    {
        // Arrange
        MockAutocomplete(PlacesBuilders.Prediction("p0"));
        MockGetPlace(PlacesBuilders.Place("p0", movedPlaceId: "moved-1"));
        var service = CreateService();

        // Act
        var results = await service.SearchPlaces(new GooglePlacesSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        Assert.Equal("moved-1", Assert.Single(results).PlaceId);
    }

    [Fact]
    public async Task SearchPlaces_WithLocationBias_SendsCircleAroundPointToAutocomplete()
    {
        // Arrange
        MockAutocomplete(PlacesBuilders.Prediction("p0"));
        MockGetPlace(PlacesBuilders.Place("p0"));
        var bias = GeoTestData.Point(10.4, 63.4);
        var service = CreateService();

        // Act
        await service.SearchPlaces(new GooglePlacesSearchRequest { Query = "lade", Limit = null, LocationBias = bias })
            .CollectAsync();

        // Assert - Point Y/X map to latitude/longitude, fixed 25km radius.
        await _placesApi.Received(1).AutocompleteAsync(
            Arg.Is<AutocompleteRequest>(r => r.LocationBias != null
                && r.LocationBias.Circle.Center.Latitude == 63.4
                && r.LocationBias.Circle.Center.Longitude == 10.4
                && r.LocationBias.Circle.Radius == 25_000),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchPlaces_WithoutLocationBias_SendsNoLocationBias()
    {
        // Arrange
        MockAutocomplete(PlacesBuilders.Prediction("p0"));
        MockGetPlace(PlacesBuilders.Place("p0"));
        var service = CreateService();

        // Act
        await service.SearchPlaces(new GooglePlacesSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        await _placesApi.Received(1).AutocompleteAsync(
            Arg.Is<AutocompleteRequest>(r => r.LocationBias == null),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchPlaces_AutocompleteReturnsNoPredictions_YieldsNothing()
    {
        // Arrange
        MockAutocomplete();
        var service = CreateService();

        // Act
        var results = await service.SearchPlaces(new GooglePlacesSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        Assert.Empty(results);
    }

    // Build the Refit error Google's client throws on a non-success status (for example a 429 quota hit).
    private static Task<ApiException> ApiError(HttpStatusCode status)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://_placesApi.googleapis.com");
        using var response = new HttpResponseMessage(status);
        return ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    [Fact]
    public async Task SearchPlaces_AutocompleteFailsWithStatus_ThrowsProblemDetailsExceptionCarryingThatStatus()
    {
        // Arrange. Google rejects the autocomplete request because the daily quota is exhausted (429).
        var apiError = await ApiError(HttpStatusCode.TooManyRequests);
        _placesApi.AutocompleteAsync(
                Arg.Any<AutocompleteRequest>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AutocompleteResponse>(apiError));
        var service = CreateService();

        // Act + Assert. The transport error is translated to a problem-details exception with the status.
        var thrown = await Assert.ThrowsAsync<ProblemDetailsException>(async () =>
            await service.SearchPlaces(new GooglePlacesSearchRequest { Query = "lade", Limit = null }).CollectAsync());
        Assert.Equal(429, thrown.StatusCode);
        Assert.Same(apiError, thrown.InnerException);
    }

    [Fact]
    public async Task SearchPlaces_PlaceDetailsFailsWithStatus_ThrowsProblemDetailsExceptionCarryingThatStatus()
    {
        // Arrange - Autocomplete succeeds, but fetching the place details hits the quota limit.
        MockAutocomplete(PlacesBuilders.Prediction("p0"));
        var apiError = await ApiError(HttpStatusCode.TooManyRequests);
        _placesApi.GetPlaceAsync("p0", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PlaceDetailsResponse>(apiError));
        var service = CreateService();

        // Act + Assert
        var thrown = await Assert.ThrowsAsync<ProblemDetailsException>(async () =>
            await service.SearchPlaces(new GooglePlacesSearchRequest { Query = "lade", Limit = null }).CollectAsync());
        Assert.Equal(429, thrown.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FetchPlaceAsync_BlankPlaceId_ThrowsAndCallsNoApi(string placeId)
    {
        // Arrange
        var service = CreateService();

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await service.FetchPlaceAsync(placeId));
        await _placesApi.DidNotReceiveWithAnyArgs().GetPlaceAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task FetchPlaceAsync_PlaceExists_ReturnsThePlace()
    {
        // Arrange
        MockGetPlace(PlacesBuilders.Place("p0", "Addr 0"));
        var service = CreateService();

        // Act
        var result = await service.FetchPlaceAsync("p0");

        // Assert
        Assert.Equal("p0", result!.PlaceId);
        Assert.Equal("Addr 0", result.ShortFormattedAddress);
    }

    [Fact]
    public async Task FetchPlaceAsync_PlaceHasMovedPlaceId_ReturnsReplacementId()
    {
        // Arrange - A relocated place reports its replacement under movedPlaceId.
        MockGetPlace(PlacesBuilders.Place("p0", movedPlaceId: "moved-1"));
        var service = CreateService();

        // Act
        var result = await service.FetchPlaceAsync("p0");

        // Assert
        Assert.Equal("moved-1", result!.PlaceId);
    }

    [Fact]
    public async Task FetchPlaceAsync_ObsoleteIdReturnsNotFound_ReturnsNull()
    {
        // Arrange - Google treats an obsolete place ID as NOT_FOUND (HTTP 404).
        var apiError = await ApiError(HttpStatusCode.NotFound);
        _placesApi.GetPlaceAsync("gone", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PlaceDetailsResponse>(apiError));
        var service = CreateService();

        // Act
        var result = await service.FetchPlaceAsync("gone");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchPlaceAsync_PlaceMissingLocation_ReturnsNull()
    {
        // Arrange
        MockGetPlace(PlacesBuilders.Place("p0", longitude: null, latitude: null));
        var service = CreateService();

        // Act
        var result = await service.FetchPlaceAsync("p0");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchPlaceAsync_PlaceMissingShortFormattedAddress_ReturnsNull()
    {
        // Arrange
        MockGetPlace(PlacesBuilders.Place("p0", shortAddress: null));
        var service = CreateService();

        // Act
        var result = await service.FetchPlaceAsync("p0");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchPlaceAsync_FailsWithOtherStatus_ThrowsProblemDetailsExceptionCarryingThatStatus()
    {
        // Arrange - A non-404 failure (for example a server error) is a transient problem, not "gone".
        var apiError = await ApiError(HttpStatusCode.InternalServerError);
        _placesApi.GetPlaceAsync("p0", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PlaceDetailsResponse>(apiError));
        var service = CreateService();

        // Act + Assert
        var thrown = await Assert.ThrowsAsync<ProblemDetailsException>(async () => await service.FetchPlaceAsync("p0"));
        Assert.Equal(500, thrown.StatusCode);
    }
}
