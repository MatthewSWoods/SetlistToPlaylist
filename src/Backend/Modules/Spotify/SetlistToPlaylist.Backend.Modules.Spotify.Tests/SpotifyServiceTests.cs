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

namespace SetlistToPlaylist.Backend.Modules.Spotify.Tests;

public sealed class SpotifyServiceTests
{
    private const string SessionId = "test-session-id";
    private const string PlaylistId = "playlist-123";
    private const string AccessToken = "access-token";
    private const string RefreshToken = "refresh-token";

    private readonly ISpotifyApiClient _apiClient = Substitute.For<ISpotifyApiClient>();
    private readonly ISpotifyAuthClient _authClient = Substitute.For<ISpotifyAuthClient>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly SpotifyService _sut;

    public SpotifyServiceTests()
    {
        _sut = new SpotifyService(_apiClient, _authClient, _cache,
            NullLogger<SpotifyService>.Instance);
    }

    // --- GetCurrentUserIdAsync ---

    [Fact]
    public async Task GetCurrentUserIdAsync_NoTokenInCache_ReturnsFail()
    {
        SetCachedToken(null);

        var result = await _sut.GetCurrentUserIdAsync(SessionId);

        Assert.True(result.IsFailed);
        Assert.Contains("No Spotify token", result.Errors[0].Message);
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_ValidToken_ReturnsUserId()
    {
        SetCachedToken(BuildValidToken());
        _apiClient.GetCurrentUserAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "spotify-user-123" }));

        var result = await _sut.GetCurrentUserIdAsync(SessionId);

        Assert.True(result.IsSuccess);
        Assert.Equal("spotify-user-123", result.Value);
    }

    // --- PopulatePlaylistAsync ---

    [Fact]
    public async Task PopulatePlaylistAsync_AllTracksFound_YieldsFoundEventsAndCompleted()
    {
        SetCachedToken(BuildValidToken());
        _apiClient.SearchTrackAsync("Creep", "Radiohead", AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:creep"));
        _apiClient.SearchTrackAsync("Karma Police", "Radiohead", AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:karma"));
        _apiClient.AddTracksToPlaylistAsync(PlaylistId, Arg.Any<IEnumerable<string>>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        _apiClient.GetCurrentUserAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "user" }));

        var events = await CollectEventsAsync(BuildSetlist("Creep", "Karma Police"));

        var foundEvents = events.Where(e => e.Type == "track_found").ToArray();
        var completed = events.Single(e => e.Type == "completed");

        Assert.Equal(2, foundEvents.Length);
        Assert.Equal(2, completed.TrackUris?.Length);
        Assert.Empty(completed.FailedTracks ?? []);
    }

    [Fact]
    public async Task PopulatePlaylistAsync_SomeTracksNotFound_YieldsCorrectMix()
    {
        SetCachedToken(BuildValidToken());
        _apiClient.SearchTrackAsync("Creep", "Radiohead", AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:creep"));
        _apiClient.SearchTrackAsync("Mystery Song", "Radiohead", AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>(null)); // not found
        _apiClient.AddTracksToPlaylistAsync(PlaylistId, Arg.Any<IEnumerable<string>>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        _apiClient.GetCurrentUserAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "user" }));

        var events = await CollectEventsAsync(BuildSetlist("Creep", "Mystery Song"));

        Assert.Contains(events, e => e.Type == "track_found" && e.SongName == "Creep");
        Assert.Contains(events, e => e.Type == "track_failed" && e.SongName == "Mystery Song");
        var completed = events.Single(e => e.Type == "completed");
        Assert.Single(completed.TrackUris ?? []);
        Assert.Single(completed.FailedTracks ?? []);
    }

    [Fact]
    public async Task PopulatePlaylistAsync_TapeTracksSkipped_NotIncludedInEventsOrPlaylist()
    {
        SetCachedToken(BuildValidToken());
        _apiClient.AddTracksToPlaylistAsync(PlaylistId, Arg.Any<IEnumerable<string>>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        _apiClient.GetCurrentUserAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "user" }));

        var setlist = BuildSetlist();
        setlist.Sets!.Set![0].Song =
        [
            new SongDto { Name = "Real Song", Tape = false },
            new SongDto { Name = "Backing Track", Tape = true },
        ];

        _apiClient.SearchTrackAsync("Real Song", "Radiohead", AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:real"));

        var events = await CollectEventsAsync(setlist);

        // Only "Real Song" should produce an event — tape track is silently skipped
        Assert.DoesNotContain(events, e => e.SongName == "Backing Track");
        var completed = events.Single(e => e.Type == "completed");
        Assert.Equal(1, completed.Total);
    }

    [Fact]
    public async Task PopulatePlaylistAsync_ExpiredToken_RefreshesTokenAndContinues()
    {
        var expiredToken = BuildValidToken(expired: true);
        var freshToken = BuildValidToken();
        SetCachedToken(expiredToken);

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

        var events = await CollectEventsAsync(BuildSetlist("Creep"));

        await _authClient.Received(1).RefreshTokenAsync(RefreshToken, Arg.Any<CancellationToken>());
        Assert.Contains(events, e => e.Type == "completed");
    }

    [Fact]
    public async Task PopulatePlaylistAsync_RefreshFails_YieldsErrorEventAndStops()
    {
        SetCachedToken(BuildValidToken(expired: true));
        _authClient.RefreshTokenAsync(RefreshToken, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("Token refresh failed"));

        var events = await CollectEventsAsync(BuildSetlist("Creep"));

        Assert.Single(events);
        Assert.Equal("error", events[0].Type);
        await _apiClient.DidNotReceiveWithAnyArgs()
            .SearchTrackAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task PopulatePlaylistAsync_AddTracksFails_YieldsErrorEvent()
    {
        SetCachedToken(BuildValidToken());
        _apiClient.SearchTrackAsync("Creep", "Radiohead", AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:creep"));
        _apiClient.AddTracksToPlaylistAsync(PlaylistId, Arg.Any<IEnumerable<string>>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Fail("403 Forbidden"));

        var events = await CollectEventsAsync(BuildSetlist("Creep"));

        Assert.Contains(events, e => e.Type == "error");
        Assert.DoesNotContain(events, e => e.Type == "completed");
    }

    [Fact]
    public async Task PopulatePlaylistAsync_EmptySetlist_YieldsCompletedWithNoTracks()
    {
        SetCachedToken(BuildValidToken());
        _apiClient.GetCurrentUserAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "user" }));

        var events = await CollectEventsAsync(BuildSetlist(/* no songs */));

        var completed = events.Single(e => e.Type == "completed");
        Assert.Empty(completed.TrackUris ?? []);
        Assert.Empty(completed.FailedTracks ?? []);
        await _apiClient.DidNotReceiveWithAnyArgs()
            .AddTracksToPlaylistAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task PopulatePlaylistAsync_CurrentAndTotalCorrectlyReported()
    {
        SetCachedToken(BuildValidToken());
        _apiClient.SearchTrackAsync(Arg.Any<string>(), Arg.Any<string>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok<string?>("spotify:track:x"));
        _apiClient.AddTracksToPlaylistAsync(PlaylistId, Arg.Any<IEnumerable<string>>(), AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        _apiClient.GetCurrentUserAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new UserDto { Id = "user" }));

        var events = await CollectEventsAsync(BuildSetlist("Song1", "Song2", "Song3"));

        var progressEvents = events.Where(e => e.Type is "track_found" or "track_failed").ToArray();
        Assert.Equal(3, progressEvents.Length);
        Assert.Equal(3, progressEvents[0].Total);
        Assert.Equal(1, progressEvents[0].Current);
        Assert.Equal(2, progressEvents[1].Current);
        Assert.Equal(3, progressEvents[2].Current);
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

    private static AuthDto BuildValidToken(bool expired = false) => new()
    {
        AccessToken = AccessToken,
        RefreshToken = RefreshToken,
        ExpiresIn = 3600,
        ExpiryTime = expired
            ? DateTime.UtcNow.AddHours(-1)
            : DateTime.UtcNow.AddHours(1)
    };

    private static SetlistDto BuildSetlist(params string[] songNames) => new()
    {
        Artist = new SetlistFm.Abstractions.DTOs.ArtistDto { Name = "Radiohead" },
        Venue = new VenueDto { Name = "Roundhouse", City = new CityDto { Name = "London" } },
        EventDate = "15-06-2016",
        Url = "https://www.setlist.fm/setlist/radiohead/63eb7e6b.html",
        Sets = new SetsDto
        {
            Set =
            [
                new SetDto
                {
                    Song = songNames.Select(n => new SongDto { Name = n }).ToArray()
                }
            ]
        }
    };
}
