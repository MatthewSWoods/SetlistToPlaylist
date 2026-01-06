namespace SetlistToPlaylist.ApiService.Contracts.Core;

public sealed record GeneratePlaylistResponse
{
    public required string SetlistFmUrl { get; init; }
}
