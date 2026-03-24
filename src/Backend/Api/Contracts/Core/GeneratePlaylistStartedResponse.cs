namespace SetlistToPlaylist.ApiService.Contracts.Core;

public sealed record GeneratePlaylistStartedResponse
{
    public required string PlaylistId { get; init; }
    public required string PlaylistUrl { get; init; }
}
