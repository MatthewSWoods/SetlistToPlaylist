using Newtonsoft.Json;
using SetlistToPlaylist.Api.Models.SetlistFm;
using SetlistToPlaylist.Api.Models.Spotify;

namespace SetlistToPlaylist.Api.Models;

public class GeneratePlaylistResponse
{
    [JsonProperty("playlist")]
    public SpotifyPlaylist? Playlist { get; set; }
    [JsonProperty("setlist")]
    public Setlist? Setlist { get; set; }
}