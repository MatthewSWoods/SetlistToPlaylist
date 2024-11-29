using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.SetlistFm
{
    public class Venue
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("city")]
        public City? City { get; set; }
        [JsonProperty("url")]
        public string? Url { get; set; }
    }

}
