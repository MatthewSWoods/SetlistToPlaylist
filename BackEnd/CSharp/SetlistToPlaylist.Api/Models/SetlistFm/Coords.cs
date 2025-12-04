using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.SetlistFm
{
    public class Coords
    {
        [JsonProperty("lat")]
        public float Latitude { get; set; }
        [JsonProperty("long")]
        public float Longitude { get; set; }
    }

}
