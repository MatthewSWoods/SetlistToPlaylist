using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Extensions;

public static class AuthExtensions
{

    public static void SetExpiryTime(this AuthDto spotifyAuth) =>
        spotifyAuth.ExpiryTime = DateTime.UtcNow.AddSeconds((spotifyAuth?.ExpiresIn ?? 0) - 60);

    public static bool IsTokenExpired(this AuthDto spotifyAuth) =>
        DateTime.UtcNow >= (spotifyAuth.ExpiryTime ?? DateTime.UnixEpoch);

}
