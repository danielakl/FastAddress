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
