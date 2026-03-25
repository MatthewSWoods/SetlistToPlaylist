using FluentResults;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Models;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Services;

public interface ISpotifyService
{
    Task<Result<string>> GetCurrentUserIdAsync(string sessionId, CancellationToken ct = default);

    Task<Result<PlaylistDto>> CreatePlaylistAsync(
        string userId,
        SetlistDto setlist,
        bool isPublic,
        string sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for and adds tracks to the playlist, yielding a <see cref="PlaylistProgressEvent"/> per song.
    /// The final event has Type="completed". On unrecoverable error yields Type="error" and stops.
    /// </summary>
    IAsyncEnumerable<PlaylistProgressEvent> PopulatePlaylistAsync(
        string playlistId,
        SetlistDto setlist,
        string sessionId,
        CancellationToken ct = default);
}
