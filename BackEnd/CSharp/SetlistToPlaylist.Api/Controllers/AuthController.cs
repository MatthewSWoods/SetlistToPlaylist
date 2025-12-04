using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SetlistToPlaylist.Api.RestApiClients.Interfaces;
using SetlistToPlaylist.Api.Settings;

namespace SetlistToPlaylist.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : Controller
{
    private readonly ILogger<AuthController> _logger;
    private readonly ISpotifyAuthClient _authClient;
    private readonly IOptions<FrontEndClientSettings> _settings;
    
    public AuthController(
        ILogger<AuthController> logger,
        ISpotifyAuthClient authClient,
        IOptions<FrontEndClientSettings> settings)
    {
        _logger = logger;
        _authClient = authClient;
        _settings = settings;
    }

    [HttpGet("Login")]
    public IActionResult Login()
    {
        var authUrlStateless =  _authClient.CreateOAuthRequestUrl();
        var (authUrl, state) = _authClient.AddStateToOAuthRequestUrl(authUrlStateless);
        
        _logger.LogInformation($"Redirecting to Spotify Auth URL: {authUrl}");
        HttpContext.Session.SetString("spotify_auth_state", state);
        
        return Redirect(authUrl);
    }
    
    [HttpGet("Callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? error,
        [FromQuery] string? state)
    {
        // get state and remove from session
        var sessionState = HttpContext.Session.GetString("spotify_auth_state");
        HttpContext.Session.Remove("spotify_auth_state");

        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogError($"Spotify Auth returned error: {error}");
            return Redirect(_settings.Value.BaseUrl);
        }
        
        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogError("Authorization code is empty");
            return Redirect(_settings.Value.BaseUrl);
        }

        if (string.IsNullOrWhiteSpace(state) || !state.Equals(sessionState))
        {
            _logger.LogError($"Invalid state in sessionState or callbackState - callbackState {state}, sessionState {sessionState}");
            return Redirect(_settings.Value.BaseUrl);
        }
        
        var token = await _authClient.GetTokenAsync(code);
        
        HttpContext.Session.SetString("spotify_auth_token", JsonConvert.SerializeObject(token));

        return Redirect(_settings.Value.BaseUrl);
    }

    [HttpGet("Logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove("spotify_auth_token");

        return Redirect(_settings.Value.BaseUrl);
    }
    
    [HttpGet("Status")]
    public IActionResult IsLoggedIn()
    {
        var authString = HttpContext.Session.GetString("spotify_auth_token");
        return Ok(!string.IsNullOrWhiteSpace(authString) );
    }
}


