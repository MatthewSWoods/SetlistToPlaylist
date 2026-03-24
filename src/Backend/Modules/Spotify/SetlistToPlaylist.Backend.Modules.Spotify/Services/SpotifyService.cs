using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Clients;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Models;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Services;
using SetlistToPlaylist.Backend.Modules.Spotify.Extensions;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Services;

public sealed class SpotifyService : ISpotifyService
{
    private readonly ISpotifyApiClient _apiClient;
    private readonly ISpotifyAuthClient _authClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SpotifyService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SpotifyService(
        ISpotifyApiClient apiClient,
        ISpotifyAuthClient authClient,
        IDistributedCache cache,
        ILogger<SpotifyService> logger)
    {
        _apiClient = apiClient;
        _authClient = authClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<string>> GetCurrentUserIdAsync(string sessionId, CancellationToken ct = default)
    {
        var tokenResult = await GetValidTokenAsync(sessionId, ct);
        if (tokenResult.IsFailed) return tokenResult.ToResult<string>();

        var userResult = await _apiClient.GetCurrentUserAsync(tokenResult.Value.AccessToken!, ct);
        if (userResult.IsFailed) return userResult.ToResult<string>();

        return userResult.Value.Id is null
            ? Result.Fail("Spotify user id was null")
            : Result.Ok(userResult.Value.Id);
    }

    public async Task<Result<PlaylistDto>> CreatePlaylistAsync(
        string userId, SetlistDto setlist, bool isPublic, string sessionId, CancellationToken ct = default)
    {
        var tokenResult = await GetValidTokenAsync(sessionId, ct);
        if (tokenResult.IsFailed) return tokenResult.ToResult<PlaylistDto>();

        var name = BuildPlaylistName(setlist);
        var description = BuildPlaylistDescription(setlist);

        return await _apiClient.CreatePlaylistAsync(userId, name, description, isPublic,
            tokenResult.Value.AccessToken!, ct);
    }

    public async IAsyncEnumerable<PlaylistProgressEvent> PopulatePlaylistAsync(
        string playlistId, SetlistDto setlist,
        string sessionId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var tokenResult = await GetValidTokenAsync(sessionId, ct);
        if (tokenResult.IsFailed)
        {
            yield return PlaylistProgressEvent.Error("Spotify authentication failed — please log in again");
            yield break;
        }

        var auth = tokenResult.Value;
        var artistName = setlist.Artist?.Name ?? string.Empty;

        var songs = (setlist.Sets?.Set ?? [])
            .SelectMany(s => s.Song ?? [])
            .Where(s => s.Tape != true && !string.IsNullOrWhiteSpace(s.Name))
            .ToArray();

        var total = songs.Length;
        var foundUris = new List<string>(total);
        var failedTracks = new List<string>();
        var playlistDto = default(PlaylistDto);

        for (var i = 0; i < songs.Length; i++)
        {
            ct.ThrowIfCancellationRequested();

            // Refresh token if needed
            if (auth.IsTokenExpired())
            {
                var refreshResult = await _authClient.RefreshTokenAsync(auth.RefreshToken!, ct);
                if (refreshResult.IsFailed)
                {
                    yield return PlaylistProgressEvent.Error("Token refresh failed mid-populate");
                    yield break;
                }
                auth = refreshResult.Value;
                await StoreTokenAsync(sessionId, auth, ct);
            }

            var song = songs[i];
            var searchResult = await _apiClient.SearchTrackAsync(song.Name!, artistName, auth.AccessToken!, ct);

            if (searchResult.IsFailed)
            {
                _logger.LogWarning("Search API error for '{Song}': {Error}", song.Name, searchResult.Errors[0].Message);
                failedTracks.Add(song.Name!);
                yield return PlaylistProgressEvent.TrackFailed(song.Name!, i + 1, total);
                continue;
            }

            if (searchResult.Value is null)
            {
                failedTracks.Add(song.Name!);
                yield return PlaylistProgressEvent.TrackFailed(song.Name!, i + 1, total);
            }
            else
            {
                foundUris.Add(searchResult.Value);
                yield return PlaylistProgressEvent.TrackFound(song.Name!, searchResult.Value, i + 1, total);
            }
        }

        if (foundUris.Count > 0)
        {
            var addResult = await _apiClient.AddTracksToPlaylistAsync(playlistId, foundUris, auth.AccessToken!, ct);
            if (addResult.IsFailed)
            {
                yield return PlaylistProgressEvent.Error($"Failed to add tracks to playlist: {addResult.Errors[0].Message}");
                yield break;
            }
        }

        // Retrieve playlist metadata for the completed event
        var userResult = await _apiClient.GetCurrentUserAsync(auth.AccessToken!, ct);
        if (userResult.IsSuccess)
        {
            // Build a minimal PlaylistDto for the completed event
            playlistDto = new PlaylistDto
            {
                PlaylistId = playlistId,
                PlaylistName = BuildPlaylistName(setlist),
                PlaylistDescription = BuildPlaylistDescription(setlist),
                ExternalUrls = new ExternalUrlsDto { Spotify = $"https://open.spotify.com/playlist/{playlistId}" }
            };
        }

        yield return PlaylistProgressEvent.Completed(
            playlistDto ?? new PlaylistDto
            {
                PlaylistId = playlistId,
                PlaylistName = string.Empty,
                PlaylistDescription = string.Empty,
                ExternalUrls = new ExternalUrlsDto { Spotify = $"https://open.spotify.com/playlist/{playlistId}" }
            },
            foundUris.ToArray(),
            failedTracks.ToArray());
    }

    private async Task<Result<AuthDto>> GetValidTokenAsync(string sessionId, CancellationToken ct)
    {
        var auth = await GetStoredTokenAsync(sessionId, ct);
        if (auth is null) return Result.Fail("No Spotify token found — user must authenticate");

        if (auth.IsTokenExpired())
        {
            _logger.LogInformation("Refreshing expired Spotify token for session {SessionId}", sessionId);
            var refreshResult = await _authClient.RefreshTokenAsync(auth.RefreshToken!, ct);
            if (refreshResult.IsFailed) return refreshResult;
            auth = refreshResult.Value;
            await StoreTokenAsync(sessionId, auth, ct);
        }

        return Result.Ok(auth);
    }

    private async Task<AuthDto?> GetStoredTokenAsync(string sessionId, CancellationToken ct)
    {
        var json = await _cache.GetStringAsync(TokenKey(sessionId), ct);
        if (json is null) return null;
        return JsonSerializer.Deserialize<AuthDto>(json, JsonOptions);
    }

    private async Task StoreTokenAsync(string sessionId, AuthDto auth, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(auth, JsonOptions);
        var expiry = auth.ExpiryTime.HasValue
            ? auth.ExpiryTime.Value - DateTime.UtcNow
            : TimeSpan.FromHours(1);
        await _cache.SetStringAsync(TokenKey(sessionId), json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry }, ct);
    }

    private static string TokenKey(string sessionId) => $"spotify_auth:{sessionId}";

    private static string BuildPlaylistName(SetlistDto setlist)
    {
        if (!DateTime.TryParseExact(setlist.EventDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
            date = DateTime.UtcNow;

        return $"{setlist.Artist?.Name} @ {setlist.Venue?.Name} \u2014 {date:dd MMM yyyy}";
    }

    private static string BuildPlaylistDescription(SetlistDto setlist)
    {
        if (!DateTime.TryParseExact(setlist.EventDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
            date = DateTime.UtcNow;

        return $"Live at {setlist.Venue?.Name}, {setlist.Venue?.City?.Name} on {date:dd-MM-yyyy}. Setlist: {setlist.Url}";
    }
}
