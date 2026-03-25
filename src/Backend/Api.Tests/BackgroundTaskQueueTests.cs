using SetlistToPlaylist.ApiService.BackgroundServices;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.ApiService.Tests;

public sealed class BackgroundTaskQueueTests
{
    private static PopulatePlaylistJob MakeJob(string playlistId = "playlist-1") =>
        new(playlistId, new SetlistDto(), "session-1", "conn-1");

    [Fact]
    public async Task EnqueueAsync_ThenDequeue_ReturnsSameJob()
    {
        var queue = new BackgroundTaskQueue();
        var job = MakeJob();

        await queue.EnqueueAsync(job, TestContext.Current.CancellationToken);
        var dequeued = await queue.DequeueAsync(TestContext.Current.CancellationToken);

        dequeued.ShouldBe(job);
    }

    [Fact]
    public async Task EnqueueAsync_MultipleJobs_DequeuesInFifoOrder()
    {
        var queue = new BackgroundTaskQueue();
        var job1 = MakeJob("playlist-1");
        var job2 = MakeJob("playlist-2");
        var job3 = MakeJob("playlist-3");

        await queue.EnqueueAsync(job1, TestContext.Current.CancellationToken);
        await queue.EnqueueAsync(job2, TestContext.Current.CancellationToken);
        await queue.EnqueueAsync(job3, TestContext.Current.CancellationToken);

        var first = await queue.DequeueAsync(TestContext.Current.CancellationToken);
        var second = await queue.DequeueAsync(TestContext.Current.CancellationToken);
        var third = await queue.DequeueAsync(TestContext.Current.CancellationToken);

        first.PlaylistId.ShouldBe("playlist-1");
        second.PlaylistId.ShouldBe("playlist-2");
        third.PlaylistId.ShouldBe("playlist-3");
    }

    [Fact]
    public async Task DequeueAsync_WhenEmpty_CompletesOnceItemEnqueued()
    {
        var queue = new BackgroundTaskQueue();
        var job = MakeJob();

        // Start dequeue before enqueue — should block until item arrives
        var dequeueTask = queue.DequeueAsync(TestContext.Current.CancellationToken).AsTask();

        dequeueTask.IsCompleted.ShouldBeFalse();

        await queue.EnqueueAsync(job, TestContext.Current.CancellationToken);
        var result = await dequeueTask;

        result.ShouldBe(job);
    }

    [Fact]
    public async Task DequeueAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var queue = new BackgroundTaskQueue();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await queue.DequeueAsync(cts.Token));
    }
}
