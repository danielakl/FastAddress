using FastAddress.Sdk.Api;
using FastAddress.Sdk.Serialization;
using FastAddress.Web.Components;
using FastAddress.Web.Proxy;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
