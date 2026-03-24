using Microsoft.AspNetCore.SignalR;
using SetlistToPlaylist.ApiService.Hubs;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Models;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Services;

namespace SetlistToPlaylist.ApiService.BackgroundServices;

public sealed class PlaylistPopulationWorker : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlaylistPopulationWorker> _logger;

    public PlaylistPopulationWorker(
        IBackgroundTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<PlaylistPopulationWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PlaylistPopulationWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            PopulatePlaylistJob job;
            try
            {
                job = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ProcessJobAsync(job, stoppingToken);
        }

        _logger.LogInformation("PlaylistPopulationWorker stopped");
    }

    private async Task ProcessJobAsync(PopulatePlaylistJob job, CancellationToken ct)
    {
        _logger.LogInformation(
            "Processing populate job for playlist {PlaylistId}, connection {ConnectionId}",
            job.PlaylistId, job.SignalRConnectionId);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var spotifyService = scope.ServiceProvider.GetRequiredService<ISpotifyService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<PlaylistProgressHub>>();

        try
        {
            await foreach (var progressEvent in spotifyService.PopulatePlaylistAsync(
                               job.PlaylistId, job.Setlist, job.SessionId, ct))
            {
                await PlaylistProgressHub.SendProgressAsync(hubContext, job.SignalRConnectionId, progressEvent, ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Populate job cancelled for playlist {PlaylistId}", job.PlaylistId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing populate job for playlist {PlaylistId}", job.PlaylistId);
            try
            {
                await PlaylistProgressHub.SendProgressAsync(
                    hubContext, job.SignalRConnectionId,
                    PlaylistProgressEvent.Error("An unexpected error occurred"),
                    ct);
            }
            catch
            {
                // SignalR client may have disconnected — swallow
            }
        }
    }
}
