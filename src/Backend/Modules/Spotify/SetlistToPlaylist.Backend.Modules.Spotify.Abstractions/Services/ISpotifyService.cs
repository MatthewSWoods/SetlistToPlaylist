using FluentResults;
using SetlistToPlaylist.ApiService.Contracts.Spotify;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Services;

public interface ISpotifyService
{
    public Task<Result<PlaylistDto>> CreatePlaylistAsync(AuthDto spotifyAuth, CreatePlaylistRequest createPlaylistRequest);
    public Task<Result<(string[] successfulTrackIds, string[] failedTrackIds)>> SearchTracksAsync(AuthDto spotifyAuth, string spotifyPlaylistId);
    public Task<Result> AddTracksToPlaylistAsync(AuthDto spotifyAuth, string spotifyPlaylistId, IEnumerable<string> trackIds);
}
