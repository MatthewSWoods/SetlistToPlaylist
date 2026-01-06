using Microsoft.AspNetCore.Mvc;
using SetlistToPlaylist.ApiService.Contracts.Core;

namespace SetlistToPlaylist.ApiService.Controllers;

public class SetlistToPlaylistController : Controller
{
    private readonly ILogger<SetlistToPlaylistController> _logger;

    public SetlistToPlaylistController(ILogger<SetlistToPlaylistController> logger)
    {
        _logger = logger;
    }

    [HttpPost("GeneratePlaylist")]
    public async Task<IActionResult> GeneratePlaylistAsync(
        [FromBody] string setlistSource,
        [FromBody] string playlistType,
        [FromBody] string setlistFmUrl)
    {
        _logger.LogInformation("GeneratePlaylist endpoint called.");

        // Implementation goes here

        var result = new GeneratePlaylistResponse
        {
            SetlistFmUrl = setlistFmUrl
        };

        return Ok(result);
    }

    [HttpPost("PopulatePlaylist")]
    public async Task<IActionResult> PopulatePlaylistAsync(
        [FromBody] string playlistType,
        [FromBody] string playlistId)
    {
        _logger.LogInformation("PopulatePlaylist endpoint called.");

        // Implementation goes here

        return Ok();
    }
}
