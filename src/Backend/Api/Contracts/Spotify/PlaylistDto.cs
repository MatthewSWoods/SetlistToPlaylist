using System.Text.Json.Serialization;

namespace SetlistToPlaylist.ApiService.Contracts.Spotify;

public class PlaylistDto
{
    [JsonPropertyName("id")]
    public required string PlaylistId { get; set; }

    [JsonPropertyName("name")]
    public required string PlaylistName { get; set; }

    [JsonPropertyName("description")]
    public required string PlaylistDescription { get; set; }

    [JsonPropertyName("external_urls")]
    public required ExternalUrlsDto ExternalUrls { get; set; }
}
