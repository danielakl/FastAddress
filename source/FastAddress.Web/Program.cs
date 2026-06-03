using FastAddress.Sdk.Api;
using FastAddress.Sdk.Logging;
using FastAddress.Sdk.Serialization;
using FastAddress.Web.Components;
using FastAddress.Web.Proxy;
using FastAddress.Web.State;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Route all logging through Serilog - always console and Seq if configured
builder.Host.UseFastAddressSerilog();

builder.Services.Configure<SeqLoggingOptions>(configuration.GetSection(SeqLoggingOptions.ConfigKey));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Per-circuit state shared between the search box and the map.
builder.Services.AddScoped<SearchState>();

// Serialize the proxy's own request/response (incl. GeoJSON points) exactly as the API does.
builder.Services.ConfigureHttpJsonOptions(options => FastAddressJsonOptions.Configure(options.SerializerOptions));

// Typed client for the upstream FastAddress API that the same-origin proxy forwards to.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
builder.Services.AddFastAddressApiClient(new Uri(apiBaseUrl, UriKind.Absolute));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapAddressProxyEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
