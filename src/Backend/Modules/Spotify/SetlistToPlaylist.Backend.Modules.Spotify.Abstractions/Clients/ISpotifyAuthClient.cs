using FluentResults;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Clients;

public interface ISpotifyAuthClient
{
    public Task<Result<AuthDto>> GetTokenAsync(string code);
    public Task<Result<AuthDto>> RefreshTokenAsync(AuthDto spotifyAuth);
    public Task<Result<string>> CreateOAuthRequestUrl();
    public Task<Result<(string, string)>> AddStateToOAuthRequestUrl(string url);
}
