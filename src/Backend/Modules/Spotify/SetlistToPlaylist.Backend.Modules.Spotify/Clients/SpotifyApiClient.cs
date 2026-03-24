using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using FluentResults;
using Microsoft.Extensions.Logging;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Clients;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Clients;

public sealed class SpotifyApiClient : ISpotifyApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SpotifyApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SpotifyApiClient(HttpClient httpClient, ILogger<SpotifyApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(string accessToken, CancellationToken ct = default)
    {
        using var request = BuildGet("me", accessToken);
        var response = await SendAsync(request, ct);
        if (response.IsFailed) return response.ToResult<UserDto>();

        var user = await DeserializeAsync<UserDto>(response.Value, ct);
        return user is null ? Result.Fail("Failed to deserialize user profile") : Result.Ok(user);
    }

    public async Task<Result<PlaylistDto>> CreatePlaylistAsync(
        string userId, string name, string description, bool isPublic,
        string accessToken, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new
        {
            name,
            description,
            @public = isPublic,
            collaborative = false
        }, options: JsonOptions);

        using var request = BuildPost($"users/{Uri.EscapeDataString(userId)}/playlists", accessToken, body);
        var response = await SendAsync(request, ct);
        if (response.IsFailed) return response.ToResult<PlaylistDto>();

        var playlist = await DeserializeAsync<PlaylistDto>(response.Value, ct);
        return playlist is null ? Result.Fail("Failed to deserialize playlist") : Result.Ok(playlist);
    }

    public async Task<Result<string?>> SearchTrackAsync(
        string songName, string artistName, string accessToken, CancellationToken ct = default)
    {
        // First pass: strict query with artist
        var query = HttpUtility.UrlEncode($"track:{NormalizeSongName(songName)} artist:{artistName}");
        var uri = $"search?q={query}&type=track&limit=1";

        using var request1 = BuildGet(uri, accessToken);
        var response1 = await SendAsync(request1, ct);
        if (response1.IsFailed) return response1.ToResult<string?>();

        var result1 = await DeserializeAsync<SearchResultDto>(response1.Value, ct);
        var trackUri = result1?.Tracks?.Items?.FirstOrDefault()?.Uri;
        if (!string.IsNullOrEmpty(trackUri))
        {
            _logger.LogDebug("Found track for '{Song}': {Uri}", songName, trackUri);
            return Result.Ok<string?>(trackUri);
        }

        // Second pass: without artist filter
        var query2 = HttpUtility.UrlEncode($"track:{NormalizeSongName(songName)}");
        var uri2 = $"search?q={query2}&type=track&limit=1";

        using var request2 = BuildGet(uri2, accessToken);
        var response2 = await SendAsync(request2, ct);
        if (response2.IsFailed) return response2.ToResult<string?>();

        var result2 = await DeserializeAsync<SearchResultDto>(response2.Value, ct);
        var trackUri2 = result2?.Tracks?.Items?.FirstOrDefault()?.Uri;

        _logger.LogDebug(trackUri2 is null
            ? "No track found for '{Song}'"
            : "Found track (fallback) for '{Song}': {Uri}", songName, trackUri2);

        return Result.Ok<string?>(trackUri2);
    }

    public async Task<Result> AddTracksToPlaylistAsync(
        string playlistId, IEnumerable<string> trackUris, string accessToken, CancellationToken ct = default)
    {
        const int batchSize = 100;
        var batches = trackUris.Chunk(batchSize);

        foreach (var batch in batches)
        {
            var body = JsonContent.Create(new { uris = batch }, options: JsonOptions);
            using var request = BuildPost($"playlists/{Uri.EscapeDataString(playlistId)}/items", accessToken, body);
            var response = await SendAsync(request, ct);
            if (response.IsFailed) return response.ToResult();
            response.Value.Dispose();
        }

        return Result.Ok();
    }

    private static string NormalizeSongName(string name)
    {
        // Strip common suffixes that break matching
        name = System.Text.RegularExpressions.Regex.Replace(name,
            @"\s*[\(\[]?(live|reprise|feat\..*|ft\..*|acoustic|demo|version|edit)[\)\]]?\s*$",
            string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return name.Trim();
    }

    private static HttpRequestMessage BuildGet(string relativeUri, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static HttpRequestMessage BuildPost(string relativeUri, string accessToken, HttpContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, relativeUri) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private async Task<Result<HttpResponseMessage>> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP error calling Spotify API {Uri}", request.RequestUri);
            return Result.Fail($"Spotify API request failed: {ex.Message}");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return Result.Fail("spotify_unauthorized");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Spotify API {Uri} returned {StatusCode}: {Error}",
                request.RequestUri, response.StatusCode, error);
            return Result.Fail($"Spotify API error {(int)response.StatusCode}: {error}");
        }

        return Result.Ok(response);
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);
    }
}
