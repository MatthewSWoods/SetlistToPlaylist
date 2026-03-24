namespace SetlistToPlaylist.ApiService.Contracts.Core;

public sealed record GeneratePlaylistRequest
{
    public required string SetlistUrl { get; init; }
    public required string ConnectionId { get; init; }
    public bool IsPublic { get; init; } = false;
}
