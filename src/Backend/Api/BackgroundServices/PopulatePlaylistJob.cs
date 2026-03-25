using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

namespace SetlistToPlaylist.ApiService.BackgroundServices;

public sealed record PopulatePlaylistJob(
    string PlaylistId,
    SetlistDto Setlist,
    string SessionId,
    string SignalRConnectionId
);
