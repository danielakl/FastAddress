using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace FastAddress.Sdk.Extensions;

/// <summary>
/// Extension methods for strings.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Normalize a string to a single line string.
    /// <list type="bullet">
    ///   <item>Leading and trailing white space are removed</item>
    ///   <item>Any new line characters are removed</item>
    ///   <item>Characters are normalized, see <a href="https://learn.microsoft.com/en-us/dotnet/api/system.string.normalize?view=net-7.0#system-string-normalize(system-text-normalizationform)">normalization</a></item>
    ///   <item>Optional: Capitalize all characters using the rules of the <paramref name="culture"/> provided (defaults to invariant culture)</item>
    ///   <item>Optional: Preserve sequences of white space characters</item>
    /// </list>
    /// </summary>
    /// <param name="str">The string to normalize.</param>
    /// <param name="toUpperCase">Whether to capitalize all letters using the provided <paramref name="culture"/>.</param>
    /// <param name="preserveMultipleSpaces">Whether to preserve sequences of white spaces.</param>
    /// <param name="culture">Culture to use for capitalization.</param>
    /// <returns>The normalized string.</returns>
    [return: NotNullIfNotNull(nameof(str))]
    public static string? NormalizeSingleLine(
        this string? str,
        bool toUpperCase = false,
        bool preserveMultipleSpaces = false,
        CultureInfo? culture = null
    )
    {
        if (str is null)
            return str;
        culture ??= CultureInfo.InvariantCulture;

        str = str.Trim()
            .Normalize(NormalizationForm.FormC)
            .ReplaceLineEndings("\n")
            .Replace("\n", string.Empty, StringComparison.Ordinal);

        str = preserveMultipleSpaces ? str : str.ReduceWhiteSpace();
        str = toUpperCase ? str.ToUpper(culture) : str;

        return str;
    }

    /// <summary>
    /// Normalize a string that can have line endings.
    /// </summary>
    /// <list type="bullet">
    ///   <item>Leading and trailing white space are removed</item>
    ///   <item>Line ending characters are normalized to unix line feed</item>
    ///   <item>Characters are normalized, see <a href="https://learn.microsoft.com/en-us/dotnet/api/system.string.normalize?view=net-7.0#system-string-normalize(system-text-normalizationform)">normalization</a></item>
    ///   <item>Optional: Preserve sequences of white space characters</item>
    /// </list>
    /// <param name="str">The string to normalize.</param>
    /// <param name="preserveMultipleSpaces">Whether or not to preserve sequences of white spaces.</param>
    /// <returns>The normalized string.</returns>
    [return: NotNullIfNotNull(nameof(str))]
    public static string? NormalizeMultiLine(this string? str, bool preserveMultipleSpaces = true)
    {
        if (str is null)
            return str;

        str = str.Trim().Normalize(NormalizationForm.FormC).ReplaceLineEndings("\n");

        str = preserveMultipleSpaces ? str : str.ReduceWhiteSpace();

        return str;
    }

    /// <summary>
    /// Reduce sequences of white space to a single white space.
    /// </summary>
    /// <param name="str">String to reduce white space sequences.</param>
    /// <example>
    /// 'This   has    multiple   spaces' --> 'This has multiple spaces'
    /// </example>
    /// <returns>The string with its sequences of white space reduced to single white spaces.</returns>
    [return: NotNullIfNotNull(nameof(str))]
    public static string? ReduceWhiteSpace(this string? str)
    {
        return InternalReduceWhiteSpace(str, removeAll: false);
    }

    [return: NotNullIfNotNull(nameof(str))]
    private static string? InternalReduceWhiteSpace(string? str, bool removeAll)
    {
        if (str is null)
        {
            return str;
        }

        var sb = new StringBuilder(str.Length);
        bool previousIsWhiteSpace = false;
        char? previousCharacter = null;
        foreach (char c in str)
        {
            if (char.IsWhiteSpace(c))
            {
                if (removeAll)
                {
                    continue;
                }

                if (previousIsWhiteSpace && c == previousCharacter)
                {
                    continue;
                }

                previousIsWhiteSpace = true;
                previousCharacter = c;
            }
            else
            {
                previousIsWhiteSpace = false;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
