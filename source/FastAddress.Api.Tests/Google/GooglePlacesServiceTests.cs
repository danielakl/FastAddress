using FastAddress.Api.Vendors.Google.Places;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;
using FastAddress.TestUtilities;

using NSubstitute;

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
    public async Task Search_MultiplePredictions_PreservesAutocompleteRankAsOrderScore()
    {
        // Arrange
        GivenAutocomplete(PlacesBuilders.Prediction("p0"), PlacesBuilders.Prediction("p1"), PlacesBuilders.Prediction("p2"));
        GivenPlace(PlacesBuilders.Place("p0", "Addr 0"));
        GivenPlace(PlacesBuilders.Place("p1", "Addr 1"));
        GivenPlace(PlacesBuilders.Place("p2", "Addr 2"));
        var service = CreateService();

        // Act
        var results = await service.Search(new AddressSearchRequest { Query = "lade", Limit = null })
            .CollectAsync();

        // Assert
        var ordered = results.OrderBy(r => r.OrderScore).ToList();
        Assert.Equal(new[] { "p0", "p1", "p2" }, ordered.Select(r => r.PlaceId));
        Assert.Equal(new[] { 0, 1, 2 }, ordered.Select(r => r.OrderScore));
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

        // Assert — point Y/X map to latitude/longitude, fixed 25km radius.
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
}
