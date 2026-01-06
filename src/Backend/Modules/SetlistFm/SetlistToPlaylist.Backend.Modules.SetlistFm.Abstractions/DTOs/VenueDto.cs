using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

public class VenueDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    /// <summary>
    /// Gets or sets the venue name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    /// <summary>
    /// Gets or sets the city name.
    /// </summary>
    [JsonPropertyName("city")]
    public CityDto? City { get; set; }
    /// <summary>
    /// Gets or sets the URL associated with this venue.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
