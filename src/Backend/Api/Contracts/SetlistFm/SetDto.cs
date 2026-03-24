using System.Text.Json.Serialization;

namespace SetlistToPlaylist.ApiService.Contracts.SetlistFm;

public class SetDto
{
    [JsonPropertyName("song")]
    public SongDto[]? Song { get; set; }
}
