using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

public sealed class ArtistDto
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

