using FastAddress.Sdk.Dto;
using FastAddress.Sdk.Extensions;
using FastAddress.Sdk.Helpers;
using FastAddress.TestUtilities;

using NetTopologySuite.Geometries;

namespace FastAddress.Sdk.Tests.Dto;

public sealed class SearchStreetAddressDtoTests
{
    private static SearchStreetAddressDto Dto(
        string? address = "Lade alle 77",
        int? limit = null,
        Point? locationBias = null) =>
        new() { Address = address, Limit = limit, LocationBias = locationBias ?? GeoTestData.Point(10.0, 63.0) };

    [Theory]
    [InlineData(null, 25)]
    [InlineData(0, 1)]
    [InlineData(25, 25)]
    [InlineData(500, 25)]
    public void WithCleaning_LimitOutOfRange_ClampsBetween1AndMaxLimit(int? limit, int expectedResult)
    {
        // Arrange
        var dto = Dto(limit: limit);

        // Act
        var cleaned = dto.WithCleaning();

        // Assert
        Assert.Equal(expectedResult, cleaned.Limit);
    }

    [Theory]
    [InlineData("  Lade alle 77  ", "Lade alle 77")]
    [InlineData(null, null)]
    public void WithCleaning_Address_NormalizesToSingleLine(string? address, string? expectedResult)
    {
        // Arrange
        var dto = Dto(address: address);

        // Act
        var cleaned = dto.WithCleaning();

        // Assert
        Assert.Equal(expectedResult, cleaned.Address);
    }

    [Fact]
    public void WithCleaning_LocationBias_RoundsToPrecisionModel()
    {
        // Arrange — a raw, high-precision point that has not been through the precision factory.
        var raw = new Point(new Coordinate(10.123456789, 63.987654321)) { SRID = SpatialHelper.Srid };
        var dto = Dto(locationBias: raw);

        // Act
        var cleaned = dto.WithCleaning();

        // Assert — rounded to 5 decimal places (~1 m).
        Assert.Equal(10.12346, cleaned.LocationBias!.X, precision: 5);
        Assert.Equal(63.98765, cleaned.LocationBias!.Y, precision: 5);
    }

    [Fact]
    public void CleanAndValidate_ValidRequest_IsValid()
    {
        // Arrange
        var dto = Dto(limit: 5);

        // Act
        var result = dto.CleanAndValidate();

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CleanAndValidate_MissingAddress_IsInvalid(string? address)
    {
        // Arrange
        var dto = Dto(address: address);

        // Act + Assert
        Assert.False(dto.CleanAndValidate().IsValid);
    }

    [Fact]
    public void CleanAndValidate_NullLocationBias_IsInvalid()
    {
        // Arrange
        var dto = new SearchStreetAddressDto { Address = "Lade alle 77", Limit = 5, LocationBias = null };

        // Act + Assert
        Assert.False(dto.CleanAndValidate().IsValid);
    }
}
