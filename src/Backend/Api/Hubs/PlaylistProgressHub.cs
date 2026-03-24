using Microsoft.AspNetCore.SignalR;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Models;

namespace SetlistToPlaylist.ApiService.Hubs;

public sealed class PlaylistProgressHub : Hub
{
    // Server-to-client only. The client calls connection.on("ReceiveProgress", handler).
    // The server uses IHubContext<PlaylistProgressHub> to push PlaylistProgressEvent instances.
    public static Task SendProgressAsync(
        IHubContext<PlaylistProgressHub> context,
        string connectionId,
        PlaylistProgressEvent progressEvent,
        CancellationToken ct = default)
        => context.Clients.Client(connectionId).SendAsync("ReceiveProgress", progressEvent, ct);
}
