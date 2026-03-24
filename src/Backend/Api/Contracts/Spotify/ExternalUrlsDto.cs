using System.Text.Json.Serialization;

namespace SetlistToPlaylist.ApiService.Contracts.Spotify;

public class ExternalUrlsDto
{
    [JsonPropertyName("spotify")]
    public required string Spotify { get; set; }
}
