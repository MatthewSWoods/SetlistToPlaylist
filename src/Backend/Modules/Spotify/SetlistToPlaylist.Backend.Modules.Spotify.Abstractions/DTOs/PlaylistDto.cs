using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

public class PlaylistDto 
{
    /// <summary>
    /// Gets or sets the unique identifier for the playlist.
    /// </summary>
    [JsonPropertyName("id")]
    public required string PlaylistId { get; set; }
    /// <summary>
    /// Gets or sets the name of the playlist.
    /// </summary>
    [JsonPropertyName("name")]
    public required string PlaylistName { get; set; }
    /// <summary>
    /// Gets or sets the description of the playlist.
    /// </summary>
    [JsonPropertyName("description")]
    public required string PlaylistDescription { get; set; }
    /// <summary>
    /// Gets or sets a collection of external URLs associated with this object.
    /// </summary>
    [JsonPropertyName("external_urls")]
    public required ExternalUrlsDto ExternalUrls { get; set; }
}
