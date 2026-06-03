using FastAddress.Sdk.Dto;
using FastAddress.Sdk.Extensions;
using FastAddress.Sdk.Helpers;
using FastAddress.TestUtilities;

using FluentValidation;
using FluentValidation.Results;
using FluentValidation.TestHelper;

using NetTopologySuite.Geometries;

namespace FastAddress.Sdk.Tests.Extensions;

public sealed class IValidatableExtensionsTests
{
    private static SearchStreetAddressDto ValidDto() =>
        new() { Address = "Lade alle 77", Limit = 5, LocationBias = GeoTestData.Point(10.0, 63.0) };

    [Fact]
    public void CleanAndValidate_InvalidDto_HasValidationErrorsForAddressAndLocationBias()
    {
        // Arrange - Empty address and a non-finite bias (a supplied point must still be valid).
        var nonFinite = new Point(new Coordinate(double.NaN, 63.0)) { SRID = SpatialHelper.Srid };
        var dto = new SearchStreetAddressDto { Address = null, Limit = null, LocationBias = nonFinite };

        // Act
        ValidationResult validationResult = dto.CleanAndValidate();

        // Assert
        var result = new TestValidationResult<SearchStreetAddressDto>(validationResult);
        result.ShouldHaveValidationErrorFor(dto => dto.Address);
        result.ShouldHaveValidationErrorFor(dto => dto.LocationBias);
    }

    [Fact]
    public void CleanAndValidate_ValidDto_HasNoValidationErrors()
    {
        // Arrange
        var dto = ValidDto();

        // Act
        ValidationResult validationResult = dto.CleanAndValidate();

        // Assert
        new TestValidationResult<SearchStreetAddressDto>(validationResult).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CleanAndValidateOrThrow_InvalidDto_ThrowsValidationException()
    {
        // Arrange
        var dto = new SearchStreetAddressDto { Address = "", Limit = null, LocationBias = null };

        // Act + Assert
        Assert.Throws<ValidationException>(() => dto.CleanAndValidateOrThrow());
    }

    [Fact]
    public void CleanAndValidateOrThrow_ValidDtoWithUncleanAddress_ReturnsCleanedInstance()
    {
        // Arrange
        var dto = ValidDto() with { Address = "  Lade alle 77  " };

        // Act
        var cleaned = dto.CleanAndValidateOrThrow();

        // Assert
        Assert.Equal("Lade alle 77", cleaned.Address);
    }
}
