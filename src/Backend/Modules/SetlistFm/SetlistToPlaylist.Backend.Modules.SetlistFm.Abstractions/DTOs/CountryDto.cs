using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

public class CountryDto
{
    /// <summary>
    /// Gets or sets the code associated with this country
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }
    /// <summary>
    /// Gets or sets the name of the country.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
