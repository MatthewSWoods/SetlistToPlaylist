using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

public class TourDto
{
    /// <summary>
    /// Get or sets the tour name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
