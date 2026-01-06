using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

public class UserDto
{
    /// <summary>
    /// Id For the Spotify User
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
