using System.Text.Json.Serialization;

namespace SetlistToPlaylist.ApiService.Contracts.SetlistFm;

public class ArtistDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
