using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.SetlistFm
{
    public class Tour
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
    }

}
