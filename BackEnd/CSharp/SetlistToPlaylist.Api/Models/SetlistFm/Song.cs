using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.SetlistFm
{
    public class Song
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
    }

}
