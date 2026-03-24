namespace SetlistToPlaylist.ApiService.BackgroundServices;

public interface IBackgroundTaskQueue
{
    ValueTask EnqueueAsync(PopulatePlaylistJob job, CancellationToken ct = default);
    ValueTask<PopulatePlaylistJob> DequeueAsync(CancellationToken ct);
}
