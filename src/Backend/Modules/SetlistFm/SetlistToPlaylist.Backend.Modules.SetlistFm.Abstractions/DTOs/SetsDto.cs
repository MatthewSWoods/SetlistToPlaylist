using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

public class SetsDto
{
    /// <summary>
    /// Gets or sets the collection of <see cref="Set"/> objects associated with this instance.
    /// </summary>
    [JsonPropertyName("set")]
    public SetDto[]? Set { get; set; }
}
