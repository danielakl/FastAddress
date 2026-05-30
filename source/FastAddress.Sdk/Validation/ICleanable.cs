namespace FastAddress.Sdk.Validation;

/// <summary>
/// Common methods for cleaning values in DTOs before validation.
/// </summary>
/// <typeparam name="TData">The data holder type.</typeparam>
public interface ICleanable<out TData>
{
    /// <summary>
    /// Cleans the values, such as string trimming, removing multiple spaces and normalizing unicode.
    /// </summary>
    /// <returns>Cleaned instance of <typeparamref name="TData"/>.</returns>
    TData WithCleaning();
}
