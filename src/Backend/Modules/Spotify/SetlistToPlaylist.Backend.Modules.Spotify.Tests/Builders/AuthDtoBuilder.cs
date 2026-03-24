using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Tests.Builders;

internal sealed class AuthDtoBuilder
{
    private string _accessToken = "test-access-token";
    private string _refreshToken = "test-refresh-token";
    private int _expiresIn = 3600;
    private bool _expired;

    public AuthDtoBuilder WithAccessToken(string token) { _accessToken = token; return this; }
    public AuthDtoBuilder WithRefreshToken(string token) { _refreshToken = token; return this; }
    public AuthDtoBuilder AsExpired() { _expired = true; return this; }

    public AuthDto Build() => new()
    {
        AccessToken = _accessToken,
        RefreshToken = _refreshToken,
        ExpiresIn = _expiresIn,
        ExpiryTime = _expired
            ? DateTime.UtcNow.AddHours(-1)
            : DateTime.UtcNow.AddHours(1)
    };
}
