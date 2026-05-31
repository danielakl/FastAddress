using FastAddress.Sdk.Dto;
using FastAddress.Sdk.Extensions;

using FluentValidation;
using FluentValidation.Results;
using FluentValidation.TestHelper;

namespace FastAddress.Sdk.Tests.Extensions;

public sealed class IValidatableExtensionsTests
{
    private static SearchAddressDto ValidDto() =>
        new() { Center = null, Address = "Lade alle 77", Radius = 1000 };

    [Fact]
    public void CleanAndValidate_InvalidDto_HasValidationErrorsForAddressAndRadius()
    {
        // Arrange
        var dto = new SearchAddressDto { Center = null, Address = null, Radius = null };

        // Act
        ValidationResult validationResult = dto.CleanAndValidate();

        // Assert
        var result = new TestValidationResult<SearchAddressDto>(validationResult);
        result.ShouldHaveValidationErrorFor(dto => dto.Address);
        result.ShouldHaveValidationErrorFor(dto => dto.Radius);
    }

    [Fact]
    public void CleanAndValidate_ValidDto_HasNoValidationErrors()
    {
        // Arrange
        var dto = ValidDto();

        // Act
        ValidationResult validationResult = dto.CleanAndValidate();

        // Assert
        new TestValidationResult<SearchAddressDto>(validationResult).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CleanAndValidateOrThrow_InvalidDto_ThrowsValidationException()
    {
        // Arrange
        var dto = new SearchAddressDto { Center = null, Address = "", Radius = null };

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
