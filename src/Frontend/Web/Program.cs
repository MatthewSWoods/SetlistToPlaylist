using SetlistToPlaylist.Web.Clients;
using SetlistToPlaylist.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<SetlistToPlaylistApiClient>(client =>
{
    // Aspire service discovery — resolves to the apiservice HTTPS endpoint
    client.BaseAddress = new Uri("https+http://apiservice");
});

// Named client used by HubConnectionBuilder — service discovery resolves "apiservice"
// via IHttpMessageHandlerFactory (https+http:// is not supported by SignalR's transport layer)
builder.Services.AddHttpClient("apiservice-hub", client =>
{
    client.BaseAddress = new Uri("https://apiservice");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseOutputCache();
app.MapStaticAssets();

// Proxy /auth/login to the API service — the browser needs to navigate to the real API
// endpoint, not the Blazor Web host. Aspire service URLs are in configuration as
// services:{name}:{scheme}:{index}.
app.MapGet("/auth/login", (IConfiguration config) =>
{
    var apiUrl = config["services:apiservice:https:0"]
              ?? config["services:apiservice:http:0"]
              ?? "https://localhost:5001";
    return Results.Redirect($"{apiUrl}/auth/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
