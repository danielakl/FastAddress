using FastAddress.Web.Components;

namespace FastAddress.Web.Tests.Components;

public sealed class ErrorModalComponentTests : BunitContext
{
    [Fact]
    public void Render_ByDefault_IsHidden()
    {
        // Act
        var cut = Render<ErrorModal>();

        // Assert
        Assert.True(string.IsNullOrWhiteSpace(cut.Markup));
    }

    [Fact]
    public async Task Show_DisplaysMessageInDialog()
    {
        // Arrange
        var cut = Render<ErrorModal>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.Show("The address service is currently unavailable."));

        // Assert
        var dialog = cut.Find("[data-testid='error-modal']");
        Assert.Contains("The address service is currently unavailable.", dialog.TextContent);
    }

    [Fact]
    public async Task Show_DisplaysTitleDetailAndTraceReference()
    {
        // Arrange
        var cut = Render<ErrorModal>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.Show(
            "Address search failed",
            "The address service responded with status 502.",
            reference: "0HN1ABCDEF"));

        // Assert
        Assert.Equal("Address search failed", cut.Find("[data-testid='error-modal-title']").TextContent.Trim());
        Assert.Equal("The address service responded with status 502.", cut.Find("[data-testid='error-modal-detail']").TextContent.Trim());
        Assert.Contains("0HN1ABCDEF", cut.Find("[data-testid='error-modal-reference']").TextContent);
    }

    [Fact]
    public async Task Show_WithBlankTitle_FallsBackToGenericHeading()
    {
        // Arrange
        var cut = Render<ErrorModal>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.Show(title: null, detail: "Boom"));

        // Assert
        Assert.Equal("Something went wrong", cut.Find("[data-testid='error-modal-title']").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-testid='error-modal-reference']"));
    }

    [Fact]
    public async Task Dismiss_HidesTheDialog()
    {
        // Arrange
        var cut = Render<ErrorModal>();
        await cut.InvokeAsync(() => cut.Instance.Show("Boom"));

        // Act
        await cut.Find("[data-testid='error-modal-dismiss']").ClickAsync(new());

        // Assert
        Assert.True(string.IsNullOrWhiteSpace(cut.Markup));
    }
}
