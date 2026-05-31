using FastAddress.Web.Components.Pages;

namespace FastAddress.Web.Tests.Components;

public sealed class CounterComponentTests : BunitContext
{
    [Fact]
    public void Counter_InitialRender_ShowsZeroCount()
    {
        // Act
        var cut = Render<Counter>();

        // Assert
        Assert.Equal("Current count: 0", cut.Find("p[role=status]").TextContent);
    }

    [Theory]
    [InlineData(1, "Current count: 1")]
    [InlineData(3, "Current count: 3")]
    public void Counter_ButtonClickedNTimes_ShowsIncrementedCount(int clicks, string expectedResult)
    {
        // Arrange
        var cut = Render<Counter>();
        var button = cut.Find("button");

        // Act
        for (var i = 0; i < clicks; i++)
        {
            button.Click();
        }

        // Assert
        Assert.Equal(expectedResult, cut.Find("p[role=status]").TextContent);
    }
}
