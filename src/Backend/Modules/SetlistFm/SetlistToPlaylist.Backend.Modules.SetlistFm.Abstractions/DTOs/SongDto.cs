using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs; 

public class SongDto
{
    /// <summary>
    /// Gets or sets the song name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
