using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SetlistToPlaylist.ApiService.BackgroundServices;
using SetlistToPlaylist.ApiService.Contracts.Core;
using SetlistToPlaylist.ApiService.Controllers;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.Services;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Services;

namespace SetlistToPlaylist.Backend.ApiService.Tests;

public sealed class SetlistToPlaylistControllerTests
{
    private const string SessionId = "test-session-id";
    private const string ConnectionId = "signalr-conn-1";
    private const string ValidSetlistUrl =
        "https://www.setlist.fm/setlist/radiohead/2016/roundhouse-63eb7e6b.html";

    private readonly ISetlistFmService _setlistFmService = Substitute.For<ISetlistFmService>();
    private readonly ISpotifyService _spotifyService = Substitute.For<ISpotifyService>();
    private readonly IBackgroundTaskQueue _queue = Substitute.For<IBackgroundTaskQueue>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly SetlistToPlaylistController _sut;

    public SetlistToPlaylistControllerTests()
    {
        _sut = new SetlistToPlaylistController(
            _setlistFmService,
            _spotifyService,
            _queue,
            _cache,
            NullLogger<SetlistToPlaylistController>.Instance);

        var session = Substitute.For<ISession>();
        session.Id.Returns(SessionId);
        session.LoadAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var httpContext = Substitute.For<HttpContext>();
        httpContext.Session.Returns(session);

        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task GeneratePlaylistAsync_EmptySetlistUrl_ReturnsBadRequest()
    {
        var request = new GeneratePlaylistRequest
        {
            SetlistUrl = "   ",
            ConnectionId = ConnectionId
        };

        var result = await _sut.GeneratePlaylistAsync(request, CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GeneratePlaylistAsync_EmptyConnectionId_ReturnsBadRequest()
    {
        var request = new GeneratePlaylistRequest
        {
            SetlistUrl = ValidSetlistUrl,
            ConnectionId = ""
        };

        var result = await _sut.GeneratePlaylistAsync(request, CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GeneratePlaylistAsync_NoTokenInCache_ReturnsUnauthorized()
    {
        _cache.GetStringAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var request = new GeneratePlaylistRequest
        {
            SetlistUrl = ValidSetlistUrl,
            ConnectionId = ConnectionId
        };

        var result = await _sut.GeneratePlaylistAsync(request, CancellationToken.None);

        result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GeneratePlaylistAsync_SetlistNotFound_ReturnsNotFound()
    {
        SetTokenInCache("{}");
        _setlistFmService.GetSetlistAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("Setlist 'abc' not found on Setlist.fm"));

        var request = new GeneratePlaylistRequest
        {
            SetlistUrl = ValidSetlistUrl,
            ConnectionId = ConnectionId
        };

        var result = await _sut.GeneratePlaylistAsync(request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GeneratePlaylistAsync_SetlistBadUrl_ReturnsBadRequest()
    {
        SetTokenInCache("{}");
        _setlistFmService.GetSetlistAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("Invalid setlist.fm URL"));

        var request = new GeneratePlaylistRequest
        {
            SetlistUrl = "https://www.setlist.fm/invalid",
            ConnectionId = ConnectionId
        };

        var result = await _sut.GeneratePlaylistAsync(request, CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GeneratePlaylistAsync_SpotifyUserIdFails_ReturnsStatusCode502()
    {
        SetTokenInCache("{}");
        _setlistFmService.GetSetlistAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new SetlistDto()));
        _spotifyService.GetCurrentUserIdAsync(SessionId, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("Spotify API error"));

        var request = new GeneratePlaylistRequest
        {
            SetlistUrl = ValidSetlistUrl,
            ConnectionId = ConnectionId
        };

        var result = await _sut.GeneratePlaylistAsync(request, CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(502);
    }

    [Fact]
    public async Task GeneratePlaylistAsync_CreatePlaylistFails_ReturnsStatusCode502()
    {
        SetTokenInCache("{}");
        _setlistFmService.GetSetlistAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new SetlistDto()));
        _spotifyService.GetCurrentUserIdAsync(SessionId, Arg.Any<CancellationToken>())
            .Returns(Result.Ok("spotify-user-123"));
        _spotifyService.CreatePlaylistAsync(
                Arg.Any<string>(), Arg.Any<SetlistDto>(), Arg.Any<bool>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("Spotify create playlist error"));

        var request = new GeneratePlaylistRequest
        {
            SetlistUrl = ValidSetlistUrl,
            ConnectionId = ConnectionId
        };

        var result = await _sut.GeneratePlaylistAsync(request, CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(502);
    }

    [Fact]
    public async Task GeneratePlaylistAsync_Success_ReturnsAcceptedWithPlaylistUrl()
    {
        SetTokenInCache("{}");
        _setlistFmService.GetSetlistAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new SetlistDto()));
        _spotifyService.GetCurrentUserIdAsync(SessionId, Arg.Any<CancellationToken>())
            .Returns(Result.Ok("spotify-user-123"));
        _spotifyService.CreatePlaylistAsync(
                Arg.Any<string>(), Arg.Any<SetlistDto>(), Arg.Any<bool>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new PlaylistDto
            {
                PlaylistId = "playlist-abc",
                PlaylistName = "Test Playlist",
                PlaylistDescription = "desc",
                ExternalUrls = new ExternalUrlsDto
                    { Spotify = "https://open.spotify.com/playlist/playlist-abc" }
            }));
        _queue.EnqueueAsync(Arg.Any<PopulatePlaylistJob>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var request = new GeneratePlaylistRequest
        {
            SetlistUrl = ValidSetlistUrl,
            ConnectionId = ConnectionId
        };

        var result = await _sut.GeneratePlaylistAsync(request, CancellationToken.None);

        var accepted = result.ShouldBeOfType<AcceptedResult>();
        var response = accepted.Value.ShouldBeOfType<GeneratePlaylistStartedResponse>();
        response.PlaylistId.ShouldBe("playlist-abc");
        response.PlaylistUrl.ShouldBe("https://open.spotify.com/playlist/playlist-abc");

        await _queue.Received(1).EnqueueAsync(
            Arg.Is<PopulatePlaylistJob>(j =>
                j.PlaylistId == "playlist-abc" && j.SignalRConnectionId == ConnectionId),
            Arg.Any<CancellationToken>());
    }

    // --- Helpers ---

    private void SetTokenInCache(string tokenJson)
    {
        _cache.GetStringAsync(
                Arg.Is<string>(k => k.StartsWith("spotify_auth:")),
                Arg.Any<CancellationToken>())
            .Returns(tokenJson);
    }
}
