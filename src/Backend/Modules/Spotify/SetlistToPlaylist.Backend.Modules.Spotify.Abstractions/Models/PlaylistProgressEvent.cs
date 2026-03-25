using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Models;

public sealed record PlaylistProgressEvent
{
    /// <summary>Values: "track_found" | "track_failed" | "completed" | "error"</summary>
    public required string Type { get; init; }
    public string? SongName { get; init; }
    public string? TrackUri { get; init; }
    public int Current { get; init; }
    public int Total { get; init; }

    // Populated on "completed"
    public PlaylistDto? Playlist { get; init; }
    public string[]? TrackUris { get; init; }
    public string[]? FailedTracks { get; init; }

    // Populated on "error"
    public string? ErrorMessage { get; init; }

    public static PlaylistProgressEvent TrackFound(string songName, string trackUri, int current, int total) =>
        new() { Type = "track_found", SongName = songName, TrackUri = trackUri, Current = current, Total = total };

    public static PlaylistProgressEvent TrackFailed(string songName, int current, int total) =>
        new() { Type = "track_failed", SongName = songName, Current = current, Total = total };

    public static PlaylistProgressEvent Completed(PlaylistDto playlist, string[] trackUris, string[] failedTracks) =>
        new() { Type = "completed", Playlist = playlist, TrackUris = trackUris, FailedTracks = failedTracks, Total = trackUris.Length + failedTracks.Length };

    public static PlaylistProgressEvent Error(string message) =>
        new() { Type = "error", ErrorMessage = message };
}
