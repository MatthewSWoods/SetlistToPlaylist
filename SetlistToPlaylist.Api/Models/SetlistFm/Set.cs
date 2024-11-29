using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.SetlistFm
{
    public class Set
    {
        [JsonProperty("song")]
        public Song[]? Song { get; set; }
    }

}
