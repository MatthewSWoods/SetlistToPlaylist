using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.SetlistFm
{
    public class City
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("state")]
        public string? State { get; set; }
        [JsonProperty("stateCode")]
        public string? StateCode { get; set; }
        [JsonProperty("coords")]
        public Coords? Coords { get; set; }
        [JsonProperty("country")]
        public Country? Country { get; set; }
    }

}
