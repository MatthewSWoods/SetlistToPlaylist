using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.Spotify
{
    public class SpotifyPlaylist
    {
        [JsonProperty("id")]
        public required string PlaylistId { get; set; }
        [JsonProperty("name")]
        public required string PlaylistName { get; set; }
        [JsonProperty("description")]
        public required string PlaylistDescription { get; set; }
        [JsonProperty("external_urls")]
        public required ExternalUrls ExternalUrls { get; set; }
    }

    public class ExternalUrls
    {
        [JsonProperty("spotify")]
        public required string Spotify { get; set; }
    }
}
