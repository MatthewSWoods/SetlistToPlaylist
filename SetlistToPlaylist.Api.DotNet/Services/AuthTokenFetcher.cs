using System.Runtime.Serialization;
using Newtonsoft.Json;
using SetlistToPlaylist.Api.Models.Spotify;
using SetlistToPlaylist.Api.RestApiClients.Interfaces;
using SetlistToPlaylist.Api.Services.Interfaces;

namespace SetlistToPlaylist.Api.Services;

public class AuthTokenFetcher : IAuthTokenFetcher
{
    private readonly ILogger<AuthTokenFetcher> _logger;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly ISpotifyAuthClient _spotifyAuthClient;
    
    public AuthTokenFetcher(
        ILogger<AuthTokenFetcher> logger,
        IHttpContextAccessor contextAccessor,
        ISpotifyAuthClient spotifyAuthClient
        )
    {
        _logger = logger;
        _contextAccessor = contextAccessor;
        _spotifyAuthClient = spotifyAuthClient;
        
    }

    public async Task<SpotifyAuth> GetSpotifyAuthFromSession()
    {
        var authString = _contextAccessor.HttpContext?.Session.GetString("spotify_auth_token");
            
        if (string.IsNullOrEmpty(authString))
        {
            throw new UnauthorizedAccessException("Spotify auth token is invalid");
        }
            
        var auth = JsonConvert.DeserializeObject<SpotifyAuth>(authString) ?? throw new SerializationException($"Unable to deserialize spotify auth token string {nameof(authString)}");
        if (auth.IsTokenExpired())
        {
            _logger.LogInformation("Auth Token is expired, refreshing token");
            auth = await _spotifyAuthClient.RefreshTokenAsync(auth);
            auth.SetExpiryTime();

            var refreshedAuthString = JsonConvert.SerializeObject(auth);
            _contextAccessor.HttpContext?.Session.SetString("spotify_auth_token", refreshedAuthString);
        }
            
        return auth;
    }
}