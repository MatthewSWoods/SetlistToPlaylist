using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using SetlistToPlaylist.Backend.Modules.Spotify.Clients;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Tests;

public sealed class SpotifyApiClientTests
{
    private const string BaseUrl = "https://api.spotify.com/v1/";
    private const string AccessToken = "test-access-token";

    private static SpotifyApiClient BuildClient(MockHttpMessageHandler handler)
    {
        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);
        return new SpotifyApiClient(httpClient, NullLogger<SpotifyApiClient>.Instance);
    }

    // --- Search ---

    [Fact]
    public async Task SearchTrackAsync_FirstPassFindsTrack_ReturnsUri()
    {
        var searchJson = BuildSearchResponse("spotify:track:abc123");
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}search*").Respond(HttpStatusCode.OK, "application/json", searchJson);

        var result = await BuildClient(handler).SearchTrackAsync("Creep", "Radiohead", AccessToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("spotify:track:abc123");
    }

    [Fact]
    public async Task SearchTrackAsync_FirstPassEmpty_SecondPassFindsTrack_ReturnsUri()
    {
        using var handler = new MockHttpMessageHandler();
        handler.Expect($"{BaseUrl}search*").Respond(HttpStatusCode.OK, "application/json", EmptySearchResponse);
        handler.Expect($"{BaseUrl}search*").Respond(HttpStatusCode.OK, "application/json", BuildSearchResponse("spotify:track:xyz789"));

        var result = await BuildClient(handler).SearchTrackAsync("Creep", "Radiohead", AccessToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("spotify:track:xyz789");
        handler.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SearchTrackAsync_BothPassesEmpty_ReturnsNullOkResult()
    {
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}search*").Respond(HttpStatusCode.OK, "application/json", EmptySearchResponse);

        var result = await BuildClient(handler).SearchTrackAsync("Unknown Song", "Unknown Artist", AccessToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public async Task SearchTrackAsync_Unauthorized_ReturnsFailWithUnauthorizedMessage()
    {
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}search*").Respond(HttpStatusCode.Unauthorized);

        var result = await BuildClient(handler).SearchTrackAsync("Creep", "Radiohead", AccessToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldBe("spotify_unauthorized");
    }

    [Fact]
    public async Task SearchTrackAsync_ServerError_ReturnsFail()
    {
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}search*").Respond(HttpStatusCode.InternalServerError);

        var result = await BuildClient(handler).SearchTrackAsync("Creep", "Radiohead", AccessToken);

        result.IsFailed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Creep (live)", "Creep")]
    [InlineData("Paranoid Android (reprise)", "Paranoid Android")]
    [InlineData("High and Dry feat. Someone", "High and Dry")]
    [InlineData("  Lucky  ", "Lucky")]
    public async Task SearchTrackAsync_NormalizesSongName_UsesNormalizedInQuery(
        string rawName, string normalizedName)
    {
        string? capturedUrl = null;
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}search*")
            .Respond(req =>
            {
                capturedUrl = req.RequestUri?.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildSearchResponse("spotify:track:test"),
                        System.Text.Encoding.UTF8, "application/json")
                };
            });

        await BuildClient(handler).SearchTrackAsync(rawName, "Radiohead", AccessToken);

        capturedUrl.ShouldNotBeNull();
        capturedUrl!.ShouldContain(Uri.EscapeDataString(normalizedName));
    }

    // --- CreatePlaylist ---

    [Fact]
    public async Task CreatePlaylistAsync_Success_ReturnsPlaylistDto()
    {
        var userId = "testuser";
        var playlistJson = """
            {
              "id": "playlist123",
              "name": "Test Playlist",
              "description": "A description",
              "external_urls": { "spotify": "https://open.spotify.com/playlist/playlist123" }
            }
            """;
        using var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, $"{BaseUrl}users/{userId}/playlists")
            .Respond(HttpStatusCode.Created, "application/json", playlistJson);

        var result = await BuildClient(handler).CreatePlaylistAsync(
            userId, "Test Playlist", "A description", false, AccessToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.PlaylistId.ShouldBe("playlist123");
        result.Value.PlaylistName.ShouldBe("Test Playlist");
    }

    // --- AddTracksToPlaylist ---

    [Fact]
    public async Task AddTracksToPlaylistAsync_SingleBatch_PostsOnce()
    {
        var uris = Enumerable.Range(1, 5).Select(i => $"spotify:track:{i}").ToList();
        var requestCount = 0;

        using var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, $"{BaseUrl}playlists/*/items")
            .Respond(req =>
            {
                requestCount++;
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("""{"snapshot_id":"snap1"}""",
                        System.Text.Encoding.UTF8, "application/json")
                };
            });

        var result = await BuildClient(handler).AddTracksToPlaylistAsync("playlist123", uris, AccessToken);

        result.IsSuccess.ShouldBeTrue();
        requestCount.ShouldBe(1);
    }

    [Fact]
    public async Task AddTracksToPlaylistAsync_MoreThan100Tracks_BatchesRequests()
    {
        var uris = Enumerable.Range(1, 101).Select(i => $"spotify:track:{i}").ToList();
        var requestCount = 0;

        using var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, $"{BaseUrl}playlists/*/items")
            .Respond(req =>
            {
                requestCount++;
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("""{"snapshot_id":"snap1"}""",
                        System.Text.Encoding.UTF8, "application/json")
                };
            });

        var result = await BuildClient(handler).AddTracksToPlaylistAsync("playlist123", uris, AccessToken);

        result.IsSuccess.ShouldBeTrue();
        requestCount.ShouldBe(2); // 100 + 1
    }

    [Fact]
    public async Task AddTracksToPlaylistAsync_ApiError_ReturnsFail()
    {
        using var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, $"{BaseUrl}playlists/*/items")
            .Respond(HttpStatusCode.Forbidden);

        var result = await BuildClient(handler)
            .AddTracksToPlaylistAsync("playlist123", ["spotify:track:1"], AccessToken);

        result.IsFailed.ShouldBeTrue();
    }

    // --- Helpers ---

    private static string BuildSearchResponse(string trackUri) => JsonSerializer.Serialize(new
    {
        tracks = new
        {
            items = new[]
            {
                new { uri = trackUri, name = "Song Name", artists = new[] { new { name = "Artist" } } }
            }
        }
    });

    private static readonly string EmptySearchResponse = JsonSerializer.Serialize(new
    {
        tracks = new { items = Array.Empty<object>() }
    });
}
