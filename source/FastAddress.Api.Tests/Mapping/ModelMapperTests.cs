using FastAddress.Api.Mapping;
using FastAddress.Api.Services.Mapping;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;
using FastAddress.Sdk.Helpers;

using NetTopologySuite.Geometries;

namespace FastAddress.Api.Tests.Mapping;

public sealed class ModelMapperTests
{
    private const string Subpremise = "subpremise";

    private static AddressComponent Component(string longText, string shortText, params string[] types) =>
        new() { LongText = longText, ShortText = shortText, Types = types };

    private static GooglePlace Result(params AddressComponent[] components) =>
        new()
        {
            PlaceId = "place-1",
            ShortFormattedAddress = "Lade alle 77 a, Trondheim",
            // Raw, high-precision point so the precision pass is observable.
            Location = new Point(new Coordinate(10.123456789, 63.987654321)) { SRID = SpatialHelper.Srid },
            Types = ["street_address"],
            AddressComponents = components,
        };

    [Fact]
    public void From_RouteAndStreetNumber_ComposesStreetLineAndNormalizedSearchText()
    {
        // Arrange
        var result = Result(
            Component("Lade alle", "Lade alle", AddressComponentTypes.Route),
            Component("77 a", "77 a", AddressComponentTypes.StreetNumber),
            Component("7041", "7041", AddressComponentTypes.PostalCode),
            Component("Trondheim", "Trondheim", AddressComponentTypes.PostalTown),
            Component("Norge", "NO", AddressComponentTypes.Country));

        // Act
        var upsert = ModelMapper.From(result);

        // Assert
        Assert.NotNull(upsert);
        Assert.Equal("place-1", upsert.GooglePlaceId);
        Assert.Equal("Lade alle 77 a", upsert.StreetLine);
        Assert.Equal("LADE ALLE 77 A", upsert.SearchText);
        Assert.Equal("7041", upsert.PostalCode);
        Assert.Equal("Trondheim", upsert.PostalTown);
        Assert.Equal("Norge", upsert.Country);
    }

    [Fact]
    public void From_SubpremiseComponent_IsExcludedFromStreetLine()
    {
        // Arrange
        var result = Result(
            Component("Lade alle", "Lade alle", AddressComponentTypes.Route),
            Component("77", "77", AddressComponentTypes.StreetNumber),
            Component("H0202", "H0202", Subpremise));

        // Act
        var upsert = ModelMapper.From(result);

        // Assert
        Assert.NotNull(upsert);
        Assert.Equal("Lade alle 77", upsert.StreetLine);
        Assert.DoesNotContain("H0202", upsert.StreetLine);
    }

    [Fact]
    public void From_NoRouteOrStreetNumber_ReturnsNull()
    {
        // Arrange - Only a postal code; nothing to build a street line from.
        var result = Result(Component("7041", "7041", AddressComponentTypes.PostalCode));

        // Act
        var upsert = ModelMapper.From(result);

        // Assert
        Assert.Null(upsert);
    }

    [Fact]
    public void From_PremiseWithoutRouteOrStreetNumber_UsesPremiseAsStreetLine()
    {
        // Arrange - A named premise (e.g. an airport) with no route/number, mirroring the
        // "Trondheim Lufthavn Værnes" case where Google omits street components.
        var result = Result(
            Component("Trondheim Lufthavn Værnes", "Trondheim Lufthavn Værnes", AddressComponentTypes.Premise),
            Component("7500", "7500", AddressComponentTypes.PostalCode),
            Component("Stjørdal", "Stjørdal", AddressComponentTypes.PostalTown));

        // Act
        var upsert = ModelMapper.From(result);

        // Assert
        Assert.NotNull(upsert);
        Assert.Equal("Trondheim Lufthavn Værnes", upsert.StreetLine);
        Assert.Equal("7500", upsert.PostalCode);
        Assert.Equal("Stjørdal", upsert.PostalTown);
    }

    [Fact]
    public void From_RouteAndPremiseBothPresent_PrefersRouteOverPremise()
    {
        // Arrange - route+number always win, the premise is only a fallback.
        var result = Result(
            Component("Lade alle", "Lade alle", AddressComponentTypes.Route),
            Component("77", "77", AddressComponentTypes.StreetNumber),
            Component("Some Building", "Some Building", AddressComponentTypes.Premise));

        // Act
        var upsert = ModelMapper.From(result);

        // Assert
        Assert.NotNull(upsert);
        Assert.Equal("Lade alle 77", upsert.StreetLine);
    }

    [Fact]
    public void From_PostalTownMissing_FallsBackToLocality()
    {
        // Arrange
        var result = Result(
            Component("Lade alle", "Lade alle", AddressComponentTypes.Route),
            Component("77", "77", AddressComponentTypes.StreetNumber),
            Component("Trondheim", "Trondheim", AddressComponentTypes.Locality));

        // Act
        var upsert = ModelMapper.From(result);

        // Assert
        Assert.NotNull(upsert);
        Assert.Equal("Trondheim", upsert.PostalTown);
    }

    [Fact]
    public void From_EmptyLongText_FallsBackToShortText()
    {
        // Arrange
        var result = Result(
            Component(string.Empty, "Lade alle", AddressComponentTypes.Route),
            Component("77", "77", AddressComponentTypes.StreetNumber));

        // Act
        var upsert = ModelMapper.From(result);

        // Assert
        Assert.NotNull(upsert);
        Assert.Equal("Lade alle 77", upsert.StreetLine);
    }

    [Fact]
    public void From_Location_IsRoundedToPrecisionModel()
    {
        // Arrange
        var result = Result(
            Component("Lade alle", "Lade alle", AddressComponentTypes.Route),
            Component("77", "77", AddressComponentTypes.StreetNumber));

        // Act
        var upsert = ModelMapper.From(result);

        // Assert
        Assert.NotNull(upsert);
        Assert.Equal(10.12346, upsert.Location.X, precision: 5);
        Assert.Equal(63.98765, upsert.Location.Y, precision: 5);
    }
}
