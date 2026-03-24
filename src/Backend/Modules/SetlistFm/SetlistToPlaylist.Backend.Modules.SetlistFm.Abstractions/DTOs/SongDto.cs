using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs; 

public class SongDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tape")]
    public bool? Tape { get; set; }

    [JsonPropertyName("info")]
    public string? Info { get; set; }
}
