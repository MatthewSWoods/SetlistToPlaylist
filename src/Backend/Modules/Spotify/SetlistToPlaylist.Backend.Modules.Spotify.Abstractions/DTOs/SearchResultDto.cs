using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

/// <summary>Top-level response from GET /search?type=track</summary>
public sealed record SearchResultDto
{
    [JsonPropertyName("tracks")]
    public TracksDto? Tracks { get; init; }
}
