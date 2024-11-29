using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models.SetlistFm
{
    public class Sets
    {
        [JsonProperty("set")]
        public Set[]? Set { get; set; }
    }

}
