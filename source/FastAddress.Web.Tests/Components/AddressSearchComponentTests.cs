using FastAddress.Web.Components;
using FastAddress.Web.Models;
using FastAddress.Web.State;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
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
    private const string Locality = "[data-testid='address-search-result-locality']";

    private static readonly AddressResult[] SampleResults =
    [
        new() { StreetAddress = "Lade allé 77", PostalCode = "7041", PostalTown = "Trondheim", Latitude = 63.44, Longitude = 10.45, Score = 0.92 },
        new() { StreetAddress = "Lade allé 80", PostalCode = "7041", PostalTown = "Trondheim", Latitude = 63.45, Longitude = 10.46, Score = 0.81 },
    ];

    public AddressSearchComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<SearchState>();
    }

    private void SetupSearch(AddressResult[] results) =>
        JSInterop.SetupModule(ModulePath)
            .Setup<SearchOutcome?>("search", _ => true)
            .SetResult(new SearchOutcome { Results = results });

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

        await cut.WaitForAssertionAsync(() => Assert.Contains("opacity-100", cut.Find(Counter).GetAttribute("class")));
    }

    [Fact]
    public async Task ClearButton_AppearsWithContent_AndClearsTheInput()
    {
        SetupSearch(SampleResults);
        var cut = Render<AddressSearch>();

        await cut.Find(Input).InputAsync(new ChangeEventArgs { Value = "Lade" });
        await cut.Find(ClearButton).ClickAsync(new MouseEventArgs());

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
        await cut.WaitForAssertionAsync(() => Assert.Equal(2, cut.FindAll(Result).Count));

        await cut.FindAll(Result)[0].ClickAsync(new());

        Assert.NotNull(state.SearchedLocation);
        Assert.Equal(63.44, state.SearchedLocation!.Value.Latitude, precision: 2);
    }

    [Fact]
    public async Task Search_RendersPostalSubtext_WhenPostalDataPresent()
    {
        SetupSearch(SampleResults);
        var cut = Render<AddressSearch>();

        await cut.Find(Input).InputAsync(new ChangeEventArgs { Value = "Lade" });
        await cut.WaitForAssertionAsync(() => Assert.Equal(2, cut.FindAll(Result).Count));

        // Postal town then code, per the configured display order.
        Assert.Equal("Trondheim 7041", cut.FindAll(Locality)[0].TextContent.Trim());
    }

    [Fact]
    public async Task Search_OmitsPostalSubtext_WhenNoPostalData()
    {
        AddressResult[] withoutPostal =
        [
            new() { StreetAddress = "Lade allé 77", Latitude = 63.44, Longitude = 10.45, Score = 0.92 },
        ];
        SetupSearch(withoutPostal);
        var cut = Render<AddressSearch>();

        await cut.Find(Input).InputAsync(new ChangeEventArgs { Value = "Lade" });
        await cut.WaitForAssertionAsync(() => Assert.Single(cut.FindAll(Result)));

        Assert.Empty(cut.FindAll(Locality));
    }

    [Fact]
    public async Task Search_WhenApiReturnsProblemDetails_RaisesOnErrorWithTitleAndDetail()
    {
        var problem = new ProblemDetails
        {
            Title = "Address search failed",
            Detail = "The address service responded with status 502.",
        };
        JSInterop.SetupModule(ModulePath)
            .Setup<SearchOutcome?>("search", _ => true)
            .SetResult(new SearchOutcome { Error = problem });

        ProblemDetails? error = null;
        var cut = Render<AddressSearch>(parameters => parameters
            .Add(c => c.OnError, EventCallback.Factory.Create<ProblemDetails>(this, raised => error = raised)));

        await cut.Find(Input).InputAsync(new ChangeEventArgs { Value = "Lade" });

        await cut.WaitForAssertionAsync(() => Assert.NotNull(error));
        Assert.Equal("Address search failed", error!.Title);
        Assert.Equal("The address service responded with status 502.", error.Detail);
        // No results are shown when the search fails.
        Assert.Empty(cut.FindAll(Result));
    }

    [Fact]
    public async Task Search_WhenModuleThrows_RaisesGenericOnErrorWithoutLeakingTheException()
    {
        JSInterop.SetupModule(ModulePath)
            .Setup<SearchOutcome?>("search", _ => true)
            .SetException(new JSException("network down\n    at Module.search (app.js:31:19)"));

        ProblemDetails? error = null;
        var cut = Render<AddressSearch>(parameters => parameters
            .Add(c => c.OnError, EventCallback.Factory.Create<ProblemDetails>(this, raised => error = raised)));

        await cut.Find(Input).InputAsync(new ChangeEventArgs { Value = "Lade" });

        await cut.WaitForAssertionAsync(() => Assert.NotNull(error));
        Assert.Equal("Address search failed", error!.Title);
        // The raw exception text and stack trace must never reach the user-facing message.
        Assert.DoesNotContain("network down", error.Detail);
        Assert.DoesNotContain("at Module.search", error.Detail);
    }
}
