using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.SetlistFm
{
    public class Artist
    {
        [JsonProperty("mbid")]
        public string? Mbid { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("sortName")]
        public string? SortName { get; set; }
        [JsonProperty("disambiguation")]
        public string? Disambiguation { get; set; }
        [JsonProperty("url")]
        public string? Url { get; set; }
    }

}
