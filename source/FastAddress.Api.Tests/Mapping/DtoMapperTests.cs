using FastAddress.Api.Mapping;
using FastAddress.Api.Models;
using FastAddress.TestUtilities;

namespace FastAddress.Api.Tests.Mapping;

public sealed class DtoMapperTests
{
    private static StreetAddressSearchEntry Entry(double score = 0.42d) =>
        new()
        {
            PlaceId = "place-1",
            StreetLine = "Lade alle 77",
            PostalCode = "7041",
            PostalTown = "Trondheim",
            Location = GeoTestData.Point(10.0, 63.0),
            Score = score,
            IsCacheHit = true,
        };

    [Fact]
    public void ToStreetAddressDto_Entry_CopiesStreetLineLocationAndScore()
    {
        // Arrange
        var entry = Entry(score: 0.73d);

        // Act
        var dto = DtoMapper.ToStreetAddressDto(entry);

        // Assert
        Assert.Equal(entry.StreetLine, dto.StreetAddress);
        Assert.Same(entry.Location, dto.Location);
        Assert.Equal(0.73d, dto.Score);
    }

    [Fact]
    public void ToStreetAddressDto_Entry_CopiesPostalCodeAndTown()
    {
        // Arrange
        var entry = Entry();

        // Act
        var dto = DtoMapper.ToStreetAddressDto(entry);

        // Assert
        Assert.Equal("7041", dto.PostalCode);
        Assert.Equal("Trondheim", dto.PostalTown);
    }
}
