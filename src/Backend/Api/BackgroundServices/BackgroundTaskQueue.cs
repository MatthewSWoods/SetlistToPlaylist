using System.Threading.Channels;

namespace SetlistToPlaylist.ApiService.BackgroundServices;

public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<PopulatePlaylistJob> _channel =
        Channel.CreateUnbounded<PopulatePlaylistJob>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

    public async ValueTask EnqueueAsync(PopulatePlaylistJob job, CancellationToken ct = default)
        => await _channel.Writer.WriteAsync(job, ct);

    public async ValueTask<PopulatePlaylistJob> DequeueAsync(CancellationToken ct)
        => await _channel.Reader.ReadAsync(ct);
}
