using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Extensions;

public static class AuthExtensions
{

    public static void SetExpiryTime(this AuthDto spotifyAuth) => spotifyAuth.ExpiryTime = DateTime.Now.AddSeconds(spotifyAuth?.ExpiresIn ?? 0);

    public static bool IsTokenExpired(this AuthDto spotifyAuth)
    {
        return DateTime.Now >= spotifyAuth.ExpiryTime;
    }

}
