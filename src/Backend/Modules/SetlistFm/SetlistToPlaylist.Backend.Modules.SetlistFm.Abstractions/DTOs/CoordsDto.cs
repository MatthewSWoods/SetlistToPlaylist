using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

public class CoordsDto
{
    /// <summary>
    /// Gets or sets the latitude.
    /// </summary>
    [JsonPropertyName("lat")]
    public float Latitude { get; set; }
    /// <summary>
    /// Gets or sets the longitude.
    /// </summary>
    [JsonPropertyName("long")]
    public float Longitude { get; set; }
}
