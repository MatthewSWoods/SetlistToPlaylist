using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

public class ArtistDto
{
    /// <summary>
    /// MBid of the artist.
    /// </summary>
    [JsonPropertyName("mbid")]
    public string? Mbid { get; set; }
    /// <summary>
    /// Aritst name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    /// <summary>
    /// Sort name of the artist.
    /// </summary>
    [JsonPropertyName("sortName")]
    public string? SortName { get; set; }
    /// <summary>
    /// disambiguation info for the artist.
    /// </summary>
    [JsonPropertyName("disambiguation")]
    public string? Disambiguation { get; set; }
    /// <summary>
    /// setlist.fm URL for the artist.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
