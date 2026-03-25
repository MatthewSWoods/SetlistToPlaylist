using FluentResults;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Clients;

public interface ISpotifyAuthClient
{
    Task<Result<AuthDto>> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken ct = default);
    Task<Result<AuthDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}
