using FluentResults;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Clients;

public interface ISpotifyApiClient
{
    Task<Result<UserDto>> GetCurrentUserAsync(string accessToken, CancellationToken ct = default);

    Task<Result<PlaylistDto>> CreatePlaylistAsync(
        string userId,
        string name,
        string description,
        bool isPublic,
        string accessToken,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for a track on Spotify.
    /// Returns the track URI on match, null if no match found (not an error), or Fail for API errors.
    /// </summary>
    Task<Result<string?>> SearchTrackAsync(
        string songName,
        string artistName,
        string accessToken,
        CancellationToken ct = default);

    /// <summary>
    /// Adds track URIs to a playlist using POST /playlists/{id}/items (not the deprecated /tracks endpoint).
    /// Handles batching internally — up to 100 URIs per request.
    /// </summary>
    Task<Result> AddTracksToPlaylistAsync(
        string playlistId,
        IEnumerable<string> trackUris,
        string accessToken,
        CancellationToken ct = default);
}
