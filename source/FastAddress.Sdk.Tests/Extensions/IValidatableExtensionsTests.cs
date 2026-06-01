using FastAddress.Sdk.Dto;
using FastAddress.Sdk.Extensions;
using FastAddress.TestUtilities;

using FluentValidation;
using FluentValidation.Results;
using FluentValidation.TestHelper;

namespace FastAddress.Sdk.Tests.Extensions;

public sealed class IValidatableExtensionsTests
{
    private static SearchStreetAddressDto ValidDto() =>
        new() { Address = "Lade alle 77", Limit = 5, LocationBias = GeoTestData.Point(10.0, 63.0) };

    [Fact]
    public void CleanAndValidate_InvalidDto_HasValidationErrorsForAddressAndLocationBias()
    {
        // Arrange
        var dto = new SearchStreetAddressDto { Address = null, Limit = null, LocationBias = null };

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
