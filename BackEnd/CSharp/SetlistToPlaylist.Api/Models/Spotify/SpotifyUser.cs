using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.Spotify
{
    public class SpotifyUser
    {
        // class for current user profile
        [JsonProperty("id")]
        public string? Id { get; set; }
    }
}
