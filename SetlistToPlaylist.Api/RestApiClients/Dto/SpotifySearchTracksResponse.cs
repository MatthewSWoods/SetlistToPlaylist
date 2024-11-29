using Newtonsoft.Json;
using SetlistToPlaylist.Api.Models.SetlistFm;

namespace SetlistToPlaylist.Api.RestApiClients.Dto
{
    public class SpotifySearchTracksResponse
    {
        [JsonProperty("tracks")]
        public Tracks? Tracks { get; set; }
    }
    
    public class Tracks
    {
        [JsonProperty("href")]
        public string? Href { get; set; }
        [JsonProperty("limit")]
        public int? Limit { get; set; }
        [JsonProperty("next")]
        public string? Next { get; set; }
        [JsonProperty("offset")]
        public int? Offset { get; set; }
        [JsonProperty("previous")]
        public string? Previous { get; set; }
        [JsonProperty("total")]
        public int? Total { get; set; }
        [JsonProperty("items")]
        public Item[]? Items { get; set; }
    }
    
    public class SpotifyArtist
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
    }

    public class Item
    {
        [JsonProperty("id")] 
        public string? Id { get; set; }
        [JsonProperty("uri")] 
        public string? Uri { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("artists")]
        public SpotifyArtist[]? Artists { get; set; }
    }
}
