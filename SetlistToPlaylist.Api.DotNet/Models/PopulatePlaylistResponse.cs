using Newtonsoft.Json;

namespace SetlistToPlaylist.Api.Models;

public class PopulatePlaylistResponse
{
    [JsonProperty("track_uris")]
    public string[]? TrackUris { get; set; }
    [JsonProperty("failed_tracks")]
    public string[]? FailedTracks { get; set; }
}