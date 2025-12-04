using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.RestApiClients.Dto
{
    public class SpotifyCreatePlaylistRequest
    {
        [JsonProperty("name")]
        public required string Name { get; set; }
        [JsonProperty("public")]
        public bool isPublic { get; set; } = false;
        [JsonProperty("collaborative")]
        public bool isCollaborative { get; set; } = false;
        [JsonProperty("description")]
        public string? Description { get; set; }
    }
}
