using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.SetlistFm
{
    public class Setlist
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("versionId")]
        public string? VersionId { get; set; }
        [JsonProperty("eventDate")]
        public string? EventDate { get; set; }
        [JsonProperty("artist")]
        public Artist? Artist { get; set; }
        [JsonProperty("venue")]
        public Venue? Venue { get; set; }
        [JsonProperty("tour")]
        public Tour? Tour { get; set; }
        [JsonProperty("sets")]
        public Sets? Sets { get; set; }
        [JsonProperty("url")]
        public string? Url { get; set; }
    }
}
