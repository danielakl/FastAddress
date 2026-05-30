using FastAddress.Sdk.Validation;

using FluentValidation;

using NetTopologySuite.Geometries;

namespace FastAddress.Sdk.Dto;

public sealed record SearchAddressDto : IValidatable<SearchAddressDto>
{
    public string? Address { get; init; }
    public int? Radius { get; init; }
    public int? Limit { get; init; }
    public required Point? Center { get; init; }

    /// <inheritdoc/>
    public SearchAddressDto WithCleaning()
    {
        return new SearchAddressDto
        {
            Address = Address?.Trim(),
            Limit = Math.Clamp(Limit ?? 25, 1, 100),
            Radius = Radius is null ? null : Math.Clamp(Radius.Value, 500, 50_000),
            Center = Center
        };
    }

    /// <inheritdoc/>
    public static IValidator<SearchAddressDto> GetValidator()
    {
        return new InlineValidator<SearchAddressDto>
        {
            v => v.RuleFor(dto => dto.Address).NotEmpty().MaximumLength(50),
            v => v.RuleFor(dto => dto.Radius).NotEmpty(),
            v => v.RuleFor(dto => dto.Limit).NotEmpty()
        };
    }
}
