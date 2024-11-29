using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.SetlistFm
{
    public class Country
    {
        [JsonProperty("code")]
        public string? Code { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
    }

}
