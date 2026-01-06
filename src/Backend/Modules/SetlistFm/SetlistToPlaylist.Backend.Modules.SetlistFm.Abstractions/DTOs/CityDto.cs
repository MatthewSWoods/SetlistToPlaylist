using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

public class CityDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the city.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    /// <summary>
    /// Gets or sets the name of the city.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    /// <summary>
    /// Gets or sets the state city is located in.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }
    /// <summary>
    /// State code for state city is in.
    /// </summary>
    [JsonPropertyName("stateCode")]
    public string? StateCode { get; set; }
    /// <summary>
    /// Coords of the city.
    /// </summary>
    [JsonPropertyName("coords")]
    public CoordsDto? Coords { get; set; }
    /// <summary>
    /// Country the city is located in.
    /// </summary>
    [JsonPropertyName("country")]
    public CountryDto? Country { get; set; }
}
