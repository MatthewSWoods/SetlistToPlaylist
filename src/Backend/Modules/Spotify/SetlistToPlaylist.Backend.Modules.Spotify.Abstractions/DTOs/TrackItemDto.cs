using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

public sealed class TrackItemDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("uri")]
    public string? Uri { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("artists")]
    public IReadOnlyList<ArtistDto>? Artists { get; init; }
}
