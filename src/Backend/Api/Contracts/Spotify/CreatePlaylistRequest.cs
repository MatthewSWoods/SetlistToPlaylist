using System.Text.Json.Serialization;

namespace SetlistToPlaylist.ApiService.Contracts.Spotify;

public class CreatePlaylistRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("public")]
    public bool isPublic { get; set; } = false;
    [JsonPropertyName("collaborative")]
    public bool isCollaborative { get; set; } = false;
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
