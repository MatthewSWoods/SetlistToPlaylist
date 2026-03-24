using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using SetlistToPlaylist.ApiService.BackgroundServices;
using SetlistToPlaylist.ApiService.Contracts.Core;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.Services;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Services;

namespace SetlistToPlaylist.ApiService.Controllers;

[ApiController]
[Route("api/v1/setlist")]
public sealed class SetlistToPlaylistController : ControllerBase
{
    private const string SpotifyAuthKeyPrefix = "spotify_auth:";

    private readonly ISetlistFmService _setlistFmService;
    private readonly ISpotifyService _spotifyService;
    private readonly IBackgroundTaskQueue _queue;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SetlistToPlaylistController> _logger;

    public SetlistToPlaylistController(
        ISetlistFmService setlistFmService,
        ISpotifyService spotifyService,
        IBackgroundTaskQueue queue,
        IDistributedCache cache,
        ILogger<SetlistToPlaylistController> logger)
    {
        _setlistFmService = setlistFmService;
        _spotifyService = spotifyService;
        _queue = queue;
        _cache = cache;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GeneratePlaylistAsync(
        [FromBody] GeneratePlaylistRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SetlistUrl))
            return BadRequest(new { error = "SetlistUrl is required" });

        if (string.IsNullOrWhiteSpace(request.ConnectionId))
            return BadRequest(new { error = "ConnectionId is required" });

        string sessionId;
        var clientKeyHeader = HttpContext.Request.Headers["X-Client-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(clientKeyHeader))
        {
            sessionId = clientKeyHeader;
        }
        else
        {
            await HttpContext.Session.LoadAsync(ct);
            sessionId = HttpContext.Session.Id;
        }

        var token = await _cache.GetStringAsync($"{SpotifyAuthKeyPrefix}{sessionId}", ct);
        if (token is null)
        {
            _logger.LogInformation("Unauthenticated generate request from session {SessionId}", sessionId);
            return Unauthorized(new { error = "Not authenticated with Spotify", redirectTo = "/auth/login" });
        }

        _logger.LogInformation("Generating playlist for URL {Url}, session {SessionId}",
            request.SetlistUrl, sessionId);

        var setlistResult = await _setlistFmService.GetSetlistAsync(request.SetlistUrl, ct);
        if (setlistResult.IsFailed)
        {
            var msg = setlistResult.Errors[0].Message;
            _logger.LogWarning("Setlist fetch failed: {Error}", msg);
            return msg.Contains("not found")
                ? NotFound(new { error = msg })
                : BadRequest(new { error = msg });
        }

        var setlist = setlistResult.Value;
        var userIdResult = await _spotifyService.GetCurrentUserIdAsync(sessionId, ct);
        if (userIdResult.IsFailed)
            return StatusCode(502, new { error = $"Spotify error: {userIdResult.Errors[0].Message}" });

        var playlistResult = await _spotifyService.CreatePlaylistAsync(
            userIdResult.Value, setlist, request.IsPublic, sessionId, ct);
        if (playlistResult.IsFailed)
            return StatusCode(502, new { error = $"Spotify error: {playlistResult.Errors[0].Message}" });

        var playlist = playlistResult.Value;
        _logger.LogInformation("Playlist {PlaylistId} created, queuing populate job", playlist.PlaylistId);

        await _queue.EnqueueAsync(
            new PopulatePlaylistJob(playlist.PlaylistId, setlist, sessionId, request.ConnectionId), ct);

        return Accepted(new GeneratePlaylistStartedResponse
        {
            PlaylistId = playlist.PlaylistId,
            PlaylistUrl = playlist.ExternalUrls.Spotify
        });
    }
}
