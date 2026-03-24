using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Extensions;

public static class AuthExtensions
{
    public static void SetExpiryTime(this AuthDto spotifyAuth, TimeProvider timeProvider) =>
        spotifyAuth.ExpiryTime = timeProvider.GetUtcNow().UtcDateTime
            .AddSeconds((spotifyAuth?.ExpiresIn ?? 0) - 60);

    public static bool IsTokenExpired(this AuthDto spotifyAuth, TimeProvider timeProvider) =>
        timeProvider.GetUtcNow().UtcDateTime >= (spotifyAuth.ExpiryTime ?? DateTime.UnixEpoch);
}
