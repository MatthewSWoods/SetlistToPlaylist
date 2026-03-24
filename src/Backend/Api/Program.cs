using SetlistToPlaylist.ApiService.BackgroundServices;
using SetlistToPlaylist.ApiService.Hubs;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.Clients;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.Services;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Clients;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Services;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Clients;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Services;
using SetlistToPlaylist.Backend.Modules.Spotify.Clients;
using SetlistToPlaylist.Backend.Modules.Spotify.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

// Session (backed by Redis via IDistributedCache)
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.IdleTimeout = TimeSpan.FromHours(2);
});

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddSignalR();
builder.Services.AddProblemDetails();

if (builder.Environment.IsDevelopment())
    builder.Services.AddOpenApi();

// SetlistFm module
builder.Services.AddHttpClient<ISetlistFmClient, SetlistFmClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["SetlistFm:BaseUrl"] ?? "https://api.setlist.fm/rest/1.0/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("x-api-key",
        builder.Configuration["SetlistFm:ApiKey"] ?? string.Empty);
});
builder.Services.AddScoped<ISetlistFmService, SetlistFmService>();

// Spotify module
builder.Services.AddHttpClient<ISpotifyAuthClient, SpotifyAuthClient>(client =>
    client.BaseAddress = new Uri("https://accounts.spotify.com/"));

builder.Services.AddHttpClient<ISpotifyApiClient, SpotifyApiClient>(client =>
    client.BaseAddress = new Uri("https://api.spotify.com/v1/"));

builder.Services.AddScoped<ISpotifyService, SpotifyService>();

// Background queue + worker
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<PlaylistPopulationWorker>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseSession();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();
app.MapHub<PlaylistProgressHub>("/hubs/playlist");
app.MapDefaultEndpoints();

app.Run();
