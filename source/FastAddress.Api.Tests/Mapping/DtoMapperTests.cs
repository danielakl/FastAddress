using FastAddress.Api.Mapping;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;
using FastAddress.TestUtilities;

namespace FastAddress.Api.Tests.Mapping;

public sealed class DtoMapperTests
{
    private static AddressSearchResult SearchResult(int orderScore, string address = "Lade alle 77, Trondheim") =>
        new()
        {
            PlaceId = "place-1",
            OrderScore = orderScore,
            ShortFormattedAddress = address,
            Location = GeoTestData.Point(10.0, 63.0),
            Types = ["street_address"],
            AddressComponents = Array.Empty<AddressComponent>(),
        };

    [Theory]
    [InlineData(0, 1d)]
    [InlineData(1, 0.5d)]
    [InlineData(3, 0.25d)]
    public void ToAddressDto_OrderScore_MapsToInverseRankScore(int orderScore, double expectedResult)
    {
        // Arrange
        var result = SearchResult(orderScore);

        // Act
        var dto = DtoMapper.ToAddressDto(result);

        // Assert
        Assert.Equal(expectedResult, dto.Score);
    }

    [Fact]
    public void ToAddressDto_Result_CopiesAddressAndLocation()
    {
        // Arrange
        var result = SearchResult(0);

        // Act
        var dto = DtoMapper.ToAddressDto(result);

        // Assert
        Assert.Equal(result.ShortFormattedAddress, dto.StreetAddress);
        Assert.Same(result.Location, dto.Point);
    }
}
