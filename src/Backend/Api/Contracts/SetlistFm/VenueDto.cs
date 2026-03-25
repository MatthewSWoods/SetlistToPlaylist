using System.Text.Json.Serialization;

namespace SetlistToPlaylist.ApiService.Contracts.SetlistFm;

public class VenueDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("city")]
    public CityDto? City { get; set; }
}
