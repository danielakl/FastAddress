using FastAddress.Sdk.Extensions;

namespace FastAddress.Sdk.Tests.Extensions;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData("  hello  ", "hello")]
    [InlineData("hello\nworld", "helloworld")]
    [InlineData("line1\r\nline2", "line1line2")]
    [InlineData("a   b    c", "a b c")]
    public void NormalizeSingleLine_WithNewlinesAndSpaces_RemovesNewlinesAndCollapsesSpaces(
        string input,
        string expectedResult)
    {
        // Act
        var result = input.NormalizeSingleLine();

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void NormalizeSingleLine_NullInput_ReturnsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input.NormalizeSingleLine();

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("abc", "ABC")]
    [InlineData("gått", "GÅTT")]
    public void NormalizeSingleLine_ToUpperCase_CapitalizesUsingInvariantCulture(
        string input,
        string expectedResult)
    {
        // Act
        var result = input.NormalizeSingleLine(toUpperCase: true);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("a   b", "a   b")]
    public void NormalizeSingleLine_PreserveMultipleSpaces_KeepsSpaceSequences(
        string input,
        string expectedResult)
    {
        // Act
        var result = input.NormalizeSingleLine(preserveMultipleSpaces: true);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("This   has    multiple   spaces", "This has multiple spaces")]
    [InlineData("no extra spaces", "no extra spaces")]
    [InlineData("", "")]
    public void ReduceWhiteSpace_SequencesOfSameWhitespace_CollapsesToSingle(
        string input,
        string expectedResult)
    {
        // Act
        var result = input.ReduceWhiteSpace();

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void ReduceWhiteSpace_NullInput_ReturnsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input.ReduceWhiteSpace();

        // Assert
        Assert.Null(result);
    }
}
