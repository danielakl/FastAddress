using FluentValidation;

namespace FastAddress.Sdk.Validation;

/// <summary>
/// Common methods for cleaning and validating values in DTOs.
/// </summary>
/// <typeparam name="TData">The data holder type.</typeparam>
public interface IValidatable<TData> : ICleanable<TData> where TData : IValidatable<TData>
{
    /// <summary>
    /// Get validator for data.
    /// </summary>
    /// <exception cref="FluentValidation.ValidationException">One or more values were invalid.</exception>
    /// <returns>A validator for objects of type <typeparamref name="TData"/>.</returns>
    static abstract IValidator<TData> GetValidator();
}
