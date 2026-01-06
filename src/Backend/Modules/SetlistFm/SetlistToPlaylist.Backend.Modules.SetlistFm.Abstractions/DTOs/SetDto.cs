using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

public class SetDto
{
    /// <summary>
    /// Gets or sets the collection of songs associated with this set.
    /// </summary>
    [JsonPropertyName("song")]
    public SongDto[]? Song { get; set; }
}
