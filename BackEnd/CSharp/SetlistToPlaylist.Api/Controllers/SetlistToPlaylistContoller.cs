using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SetlistToPlaylist.Api.Models;
using SetlistToPlaylist.Api.Models.SetlistFm;
using SetlistToPlaylist.Api.Services.Interfaces;

namespace SetlistToPlaylist.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SetlistToPlaylistController : Controller
{
    private readonly ILogger<SetlistToPlaylistController> _logger;
    private readonly IPlaylistBuilder _playlistBuilder;
    private readonly IAuthTokenFetcher _tokenFetcher;

    public SetlistToPlaylistController(
        ILogger<SetlistToPlaylistController> logger,
        IPlaylistBuilder playlistBuilder,
        IAuthTokenFetcher tokenFetcher)
    {
        _logger = logger;
        _playlistBuilder = playlistBuilder;
        _tokenFetcher = tokenFetcher;
    }

    [HttpPost("GeneratePlaylist")]
    public async Task<IActionResult> GeneratePlaylistAsync(
        [FromBody] string setlistFmUrl)
    {
        _logger.LogInformation("Generate playlist request received");

        var auth = await _tokenFetcher.GetSpotifyAuthFromSession();
        
        var setlist = await _playlistBuilder.GetSetlistAsync(setlistFmUrl);
        var playlist = await _playlistBuilder.CreatePlaylistAsync(auth, setlist);

        var result = new GeneratePlaylistResponse()
        {
            Setlist = setlist,
            Playlist = playlist
        };
        
        return Ok(JsonConvert.SerializeObject(result));
    }

    [HttpPost("PopulatePlaylist")]
    public async Task<IActionResult> PopulatePlaylistAsync(
        [FromQuery] string playlistId,
        [FromBody] Setlist setlist)
    {
        playlistId = playlistId ?? throw new ArgumentNullException(nameof(playlistId));
        setlist = setlist ?? throw new ArgumentNullException(nameof(setlist));
        
        var auth = await _tokenFetcher.GetSpotifyAuthFromSession();
        var (trackUris, failedTracks) = await _playlistBuilder.PopulatePlaylistAsync(auth, playlistId, setlist);

        var result = new PopulatePlaylistResponse()
        {
            TrackUris = trackUris,
            FailedTracks = failedTracks
        };
        
        return Ok(JsonConvert.SerializeObject(result));
    }
}
