using FastAddress.Sdk.Extensions;
using FastAddress.Sdk.Helpers;
using FastAddress.Sdk.Validation;

using FluentValidation;

using NetTopologySuite.Geometries;

namespace FastAddress.Sdk.Dto;

/// <summary>
/// Search street address DTO request.
/// </summary>
public sealed record SearchStreetAddressDto : IValidatable<SearchStreetAddressDto>
{
    private const int AddressMaxLength = 250;
    private const int MaxLimit = 25;
    private const int MinLimit = 1;

    /// <summary>Free-text street address query.</summary>
    public string? Address { get; init; }

    /// <summary>Maximum number of results to return.</summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Optional point used to bias ranking toward nearby results. It never filters results, only reorders
    /// them. When omitted, ranking falls back to pure text similarity.
    /// </summary>
    public Point? LocationBias { get; init; }

    /// <inheritdoc/>
    public SearchStreetAddressDto WithCleaning()
    {
        return new SearchStreetAddressDto
        {
            Address = Address.NormalizeSingleLine(),
            Limit = Math.Clamp(Limit ?? MaxLimit, MinLimit, MaxLimit),
            LocationBias = SpatialHelper.MakePrecise(LocationBias),
        };
    }

    /// <inheritdoc/>
    public static IValidator<SearchStreetAddressDto> GetValidator()
    {
        return new InlineValidator<SearchStreetAddressDto>
        {
            v => v.RuleFor(dto => dto.Address).NotEmpty().MaximumLength(AddressMaxLength),
            v => v.RuleFor(dto => dto.Limit).NotNull().InclusiveBetween(MinLimit, MaxLimit),
            v => v.RuleFor(dto => dto.LocationBias)
                .Must(point => point is null || SpatialHelper.IsValidPoint(point))
                .WithMessage("'{PropertyName}' must be a valid point."),
        };
    }
}
