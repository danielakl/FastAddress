using FastAddress.Sdk.Validation;

using FluentValidation;
using FluentValidation.Results;

namespace FastAddress.Sdk.Extensions;

/// <summary>
/// Extension methods for <see cref="IValidatable{TData}" />.
/// </summary>
public static class IValidatableExtensions
{
    /// <summary>
    /// The recommended way to validate DTOs. Cleans the values before validating, and throws an exception
    /// if validation fails.
    /// </summary>
    /// <param name="data">The data holder object.</param>
    /// <typeparam name="TData">The data holder type.</typeparam>
    /// <exception cref="FluentValidation.ValidationException">One or more values were invalid.</exception>
    /// <returns>A cleaned and validated instance of <typeparamref name="TData"/>.</returns>
    public static TData CleanAndValidateOrThrow<TData>(this TData data)
        where TData : IValidatable<TData>
    {
        TData cleanedData = data.WithCleaning();
        TData.GetValidator().ValidateAndThrow(cleanedData);
        return cleanedData;
    }

    /// <summary>
    /// Cleans the values before validating, then returns the validation result.
    /// </summary>
    /// <param name="data">The data holder object.</param>
    /// <typeparam name="TData">The data holder type.</typeparam>
    /// <returns>The validation result.</returns>
    public static ValidationResult CleanAndValidate<TData>(this TData data)
        where TData : IValidatable<TData>
    {
        TData cleanedData = data.WithCleaning();
        return TData.GetValidator().Validate(cleanedData);
    }
}
