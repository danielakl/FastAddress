using FastAddress.Web.Components;
using FastAddress.Web.Models;
using FastAddress.Web.State;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace FastAddress.Web.Tests.Components;

public sealed class AddressSearchComponentTests : BunitContext
{
    private const string ModulePath = "./Components/AddressSearch.razor.js";

    private const string Input = "[data-testid='address-search-input']";
    private const string Counter = "[data-testid='address-search-counter']";
    private const string ClearButton = "[data-testid='address-search-clear']";
    private const string Result = "[data-testid='address-search-result']";

    private static readonly AddressResult[] SampleResults =
    [
        new() { StreetAddress = "Lade allé 77, 7041 Trondheim", Latitude = 63.44, Longitude = 10.45, Score = 0.92 },
        new() { StreetAddress = "Lade allé 80, 7041 Trondheim", Latitude = 63.45, Longitude = 10.46, Score = 0.81 },
    ];

    public AddressSearchComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<SearchState>();
    }

    private void SetupSearch(AddressResult[] results) =>
        JSInterop.SetupModule(ModulePath).Setup<AddressResult[]>("search", _ => true).SetResult(results);

    [Fact]
    public void Counter_IsHidden_WhenEmpty()
    {
        JSInterop.SetupModule(ModulePath);

        var cut = Render<AddressSearch>();

        Assert.Contains("opacity-0", cut.Find(Counter).GetAttribute("class"));
    }

    [Fact]
    public async Task Counter_BecomesVisible_WhenQueryHasContent()
    {
        SetupSearch(SampleResults);
        var cut = Render<AddressSearch>();

        await cut.Find(Input).InputAsync(new ChangeEventArgs { Value = "Lade" });

        cut.WaitForAssertion(() => Assert.Contains("opacity-100", cut.Find(Counter).GetAttribute("class")));
    }

    [Fact]
    public async Task ClearButton_AppearsWithContent_AndClearsTheInput()
    {
        SetupSearch(SampleResults);
        var cut = Render<AddressSearch>();

        await cut.Find(Input).InputAsync(new ChangeEventArgs { Value = "Lade" });
        await cut.Find(ClearButton).ClickAsync(new());

        Assert.Empty(cut.FindAll(ClearButton));
        Assert.Contains("0/", cut.Find(Counter).TextContent);
    }

    [Fact]
    public async Task Search_RendersResults_AndSetsSearchedLocationOnSelect()
    {
        SetupSearch(SampleResults);
        var state = Services.GetRequiredService<SearchState>();
        var cut = Render<AddressSearch>();

        await cut.Find(Input).InputAsync(new ChangeEventArgs { Value = "Lade" });
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(Result).Count));

        await cut.FindAll(Result)[0].ClickAsync(new());

        Assert.NotNull(state.SearchedLocation);
        Assert.Equal(63.44, state.SearchedLocation!.Value.Latitude, precision: 2);
    }

    [Fact]
    public async Task Search_WhenModuleThrows_RaisesOnError()
    {
        JSInterop.SetupModule(ModulePath)
            .Setup<AddressResult[]>("search", _ => true)
            .SetException(new JSException("network down"));

        string? error = null;
        var cut = Render<AddressSearch>(parameters => parameters
            .Add(c => c.OnError, EventCallback.Factory.Create<string>(this, message => error = message)));

        await cut.Find(Input).InputAsync(new ChangeEventArgs { Value = "Lade" });

        cut.WaitForAssertion(() => Assert.NotNull(error));
        Assert.Contains("network down", error);
    }
}
