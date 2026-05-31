using FastAddress.Sdk.Dto;

namespace FastAddress.Sdk.Tests.Dto;

public sealed class SearchAddressDtoTests
{
    [Theory]
    [InlineData(null, 25)]
    [InlineData(0, 1)]
    [InlineData(25, 25)]
    [InlineData(500, 100)]
    public void WithCleaning_LimitOutOfRange_ClampsBetween1And100(int? limit, int expectedResult)
    {
        // Arrange
        var dto = new SearchAddressDto { Center = null, Limit = limit };

        // Act
        var cleaned = dto.WithCleaning();

        // Assert
        Assert.Equal(expectedResult, cleaned.Limit);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(100, 500)]
    [InlineData(500, 500)]
    [InlineData(1000, 1000)]
    [InlineData(99999, 50000)]
    public void WithCleaning_Radius_ClampsBetween500And50000OrKeepsNull(int? radius, int? expectedResult)
    {
        // Arrange
        var dto = new SearchAddressDto { Center = null, Radius = radius };

        // Act
        var cleaned = dto.WithCleaning();

        // Assert
        Assert.Equal(expectedResult, cleaned.Radius);
    }

    [Theory]
    [InlineData("  Lade alle 77  ", "Lade alle 77")]
    [InlineData(null, null)]
    public void WithCleaning_Address_TrimsWhitespace(string? address, string? expectedResult)
    {
        // Arrange
        var dto = new SearchAddressDto { Center = null, Address = address };

        // Act
        var cleaned = dto.WithCleaning();

        // Assert
        Assert.Equal(expectedResult, cleaned.Address);
    }
}
