using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.Spotify
{
    public class SpotifyAuth
    {
        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }
        [JsonProperty("token_type")]
        public string? TokenType { get; set; }
        [JsonProperty("expires_in")]
        public int? ExpiresIn { get; set; } = 0;
        [JsonProperty("refresh_token")]
        public string? RefreshToken { get; set; }
        public DateTime? ExpiryTime { get; set; } = DateTime.UnixEpoch;
    }

    public static class SpotifyAuthExtensions
    {
        public static void SetExpiryTime(this SpotifyAuth spotifyAuth) => spotifyAuth.ExpiryTime = DateTime.Now.AddSeconds(spotifyAuth?.ExpiresIn ?? 0);

        public static bool IsTokenExpired(this SpotifyAuth spotifyAuth)
        {
            return DateTime.Now >= spotifyAuth.ExpiryTime;
        }
    }
}