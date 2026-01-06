using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

public class ExternalUrlsDto
{
    /// <summary>
    /// Gets or sets the Spotify URI or identifier associated with the entity.
    /// </summary>
    [JsonPropertyName("spotify")]
    public required string Spotify { get; set; }
}
