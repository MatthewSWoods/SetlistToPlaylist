using System.Text;
using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Clients;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Models;
using SetlistToPlaylist.Backend.Modules.Spotify.Services;
using SetlistToPlaylist.Backend.Modules.Spotify.Tests.Builders;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Tests;

public sealed class SpotifyServiceTests
{
    private const string SessionId = "test-session-id";
    private const string PlaylistId = "playlist-123";
    private const string AccessToken = "test-access-token";
    private const string RefreshToken = "test-refresh-token";

    private readonly ISpotifyApiClient _apiClient = Substitute.For<ISpotifyApiClient>();
    private readonly ISpotifyAuthClient _authClient = Substitute.For<ISpotifyAuthClient>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly SpotifyService _sut;

    public SpotifyServiceTests()
    {
        _sut = new SpotifyService(_apiClient, _authClient, _cache,
            NullLogger<SpotifyService>.Instance, TimeProvider.System);
    }

    // --- GetCurrentUserIdAsync ---

    [Fact]
    public async Task GetCurrentUserIdAsync_NoTokenInCache_ReturnsFail()
    {
        SetCachedToken(null);

        var result = await _sut.GetCurrentUserIdAsync(SessionId, TestContext.Current.CancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("No Spotify token");
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_ValidToken_ReturnsUserId()
    {
        SetCachedToken(new AuthDtoBuilder().Build());
        _apiClient.GetCurrentUserAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "spotify-user-123" }));

        var result = await _sut.GetCurrentUserIdAsync(SessionId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("spotify-user-123");
    }

    // --- PopulatePlaylistAsync ---

    [Fact]
    public async Task PopulatePlaylistAsync_AllTracksFound_YieldsFoundEventsAndCompleted()
    {
        SetCachedToken(new AuthDtoBuilder().Build());
        _apiClient.SearchTrackAsync("Creep", "Radiohead", AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:creep"));
        _apiClient.SearchTrackAsync("Karma Police", "Radiohead", AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:karma"));
        _apiClient.AddTracksToPlaylistAsync(PlaylistId, Arg.Any<IEnumerable<string>>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        _apiClient.GetCurrentUserAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "user" }));

        var setlist = new SetlistDtoBuilder().WithSongNames("Creep", "Karma Police").Build();
        var events = await CollectEventsAsync(setlist);

        var foundEvents = events.Where(e => e.Type == "track_found").ToArray();
        var completed = events.Single(e => e.Type == "completed");

        foundEvents.Length.ShouldBe(2);
        completed.TrackUris?.Length.ShouldBe(2);
        (completed.FailedTracks ?? []).ShouldBeEmpty();
    }

    [Fact]
    public async Task PopulatePlaylistAsync_SomeTracksNotFound_YieldsCorrectMix()
    {
        SetCachedToken(new AuthDtoBuilder().Build());
        _apiClient.SearchTrackAsync("Creep", "Radiohead", AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:creep"));
        _apiClient.SearchTrackAsync("Mystery Song", "Radiohead", AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>(null));
        _apiClient.AddTracksToPlaylistAsync(PlaylistId, Arg.Any<IEnumerable<string>>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        _apiClient.GetCurrentUserAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "user" }));

        var setlist = new SetlistDtoBuilder().WithSongNames("Creep", "Mystery Song").Build();
        var events = await CollectEventsAsync(setlist);

        events.ShouldContain(e => e.Type == "track_found" && e.SongName == "Creep");
        events.ShouldContain(e => e.Type == "track_failed" && e.SongName == "Mystery Song");
        var completed = events.Single(e => e.Type == "completed");
        (completed.TrackUris ?? []).ShouldHaveSingleItem();
        (completed.FailedTracks ?? []).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task PopulatePlaylistAsync_TapeTracksSkipped_NotIncludedInEventsOrPlaylist()
    {
        SetCachedToken(new AuthDtoBuilder().Build());
        _apiClient.AddTracksToPlaylistAsync(PlaylistId, Arg.Any<IEnumerable<string>>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        _apiClient.GetCurrentUserAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "user" }));

        var setlist = new SetlistDtoBuilder()
            .WithSongs(
                new SongDto { Name = "Real Song", Tape = false },
                new SongDto { Name = "Backing Track", Tape = true })
            .Build();

        _apiClient.SearchTrackAsync("Real Song", "Radiohead", AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:real"));

        var events = await CollectEventsAsync(setlist);

        events.ShouldNotContain(e => e.SongName == "Backing Track");
        var completed = events.Single(e => e.Type == "completed");
        completed.Total.ShouldBe(1);
    }

    [Fact]
    public async Task PopulatePlaylistAsync_ExpiredToken_RefreshesTokenAndContinues()
    {
        var freshToken = new AuthDtoBuilder().Build();
        SetCachedToken(new AuthDtoBuilder().AsExpired().Build());

        _authClient.RefreshTokenAsync(RefreshToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(freshToken));
        _cache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _apiClient.SearchTrackAsync("Creep", "Radiohead", freshToken.AccessToken!, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:creep"));
        _apiClient.AddTracksToPlaylistAsync(PlaylistId, Arg.Any<IEnumerable<string>>(), freshToken.AccessToken!, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        _apiClient.GetCurrentUserAsync(freshToken.AccessToken!, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "user" }));

        var setlist = new SetlistDtoBuilder().WithSongNames("Creep").Build();
        var events = await CollectEventsAsync(setlist);

        await _authClient.Received(1).RefreshTokenAsync(RefreshToken, Arg.Any<CancellationToken>());
        events.ShouldContain(e => e.Type == "completed");
    }

    [Fact]
    public async Task PopulatePlaylistAsync_RefreshFails_YieldsErrorEventAndStops()
    {
        SetCachedToken(new AuthDtoBuilder().AsExpired().Build());
        _authClient.RefreshTokenAsync(RefreshToken, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("Token refresh failed"));

        var setlist = new SetlistDtoBuilder().WithSongNames("Creep").Build();
        var events = await CollectEventsAsync(setlist);

        events.ShouldHaveSingleItem();
        events[0].Type.ShouldBe("error");
        await _apiClient.DidNotReceiveWithAnyArgs()
            .SearchTrackAsync(default!, default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PopulatePlaylistAsync_AddTracksFails_YieldsErrorEvent()
    {
        SetCachedToken(new AuthDtoBuilder().Build());
        _apiClient.SearchTrackAsync("Creep", "Radiohead", AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:creep"));
        _apiClient.AddTracksToPlaylistAsync(PlaylistId, Arg.Any<IEnumerable<string>>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("403 Forbidden"));

        var setlist = new SetlistDtoBuilder().WithSongNames("Creep").Build();
        var events = await CollectEventsAsync(setlist);

        events.ShouldContain(e => e.Type == "error");
        events.ShouldNotContain(e => e.Type == "completed");
    }

    [Fact]
    public async Task PopulatePlaylistAsync_EmptySetlist_YieldsCompletedWithNoTracks()
    {
        SetCachedToken(new AuthDtoBuilder().Build());
        _apiClient.GetCurrentUserAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "user" }));

        var setlist = new SetlistDtoBuilder().Build();
        var events = await CollectEventsAsync(setlist);

        var completed = events.Single(e => e.Type == "completed");
        (completed.TrackUris ?? []).ShouldBeEmpty();
        (completed.FailedTracks ?? []).ShouldBeEmpty();
        await _apiClient.DidNotReceiveWithAnyArgs()
            .AddTracksToPlaylistAsync(default!, default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PopulatePlaylistAsync_CurrentAndTotalCorrectlyReported()
    {
        SetCachedToken(new AuthDtoBuilder().Build());
        _apiClient.SearchTrackAsync(Arg.Any<string>(), Arg.Any<string>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:x"));
        _apiClient.AddTracksToPlaylistAsync(PlaylistId, Arg.Any<IEnumerable<string>>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        _apiClient.GetCurrentUserAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "user" }));

        var setlist = new SetlistDtoBuilder().WithSongNames("Song1", "Song2", "Song3").Build();
        var events = await CollectEventsAsync(setlist);

        var progressEvents = events.Where(e => e.Type is "track_found" or "track_failed").ToArray();
        progressEvents.Length.ShouldBe(3);
        progressEvents[0].Total.ShouldBe(3);
        progressEvents[0].Current.ShouldBe(1);
        progressEvents[1].Current.ShouldBe(2);
        progressEvents[2].Current.ShouldBe(3);
    }

    // --- Helpers ---

    private async Task<List<PlaylistProgressEvent>> CollectEventsAsync(SetlistDto setlist)
    {
        var events = new List<PlaylistProgressEvent>();
        await foreach (var evt in _sut.PopulatePlaylistAsync(PlaylistId, setlist, SessionId))
            events.Add(evt);
        return events;
    }

    private void SetCachedToken(AuthDto? token)
    {
        var bytes = token is null
            ? null
            : Encoding.UTF8.GetBytes(JsonSerializer.Serialize(token));
        _cache.GetAsync(
                Arg.Is<string>(k => k.StartsWith("spotify_auth:")),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(bytes));
    }
}
