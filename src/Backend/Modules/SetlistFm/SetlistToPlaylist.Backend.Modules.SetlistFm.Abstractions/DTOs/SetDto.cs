using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

public class SetDto
{
    [JsonPropertyName("song")]
    public SongDto[]? Song { get; set; }

    [JsonPropertyName("encore")]
    public int? Encore { get; set; }
}
