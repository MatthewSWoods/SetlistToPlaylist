using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using SetlistToPlaylist.Api.Models.SetlistFm;
using SetlistToPlaylist.Api.Models.Spotify;
using SetlistToPlaylist.Api.RestApiClients;
using SetlistToPlaylist.Api.RestApiClients.Dto;
using SetlistToPlaylist.Api.Settings;

namespace SetlistToPlaylistTest.Api.RestApiClients
{
    public class SpotifyApiClientTests
    {
        private readonly IHttpClientFactory _mockHttpClientFactory;
        private readonly IOptions<ApiSecrets> _mockApiSecrets;
        private readonly IOptions<ApiClientSettings> _mockApiSettings;
        private readonly ILogger<SpotifyApiClient> _mockLogger;

        public SpotifyApiClientTests()
        {
            _mockHttpClientFactory = Mock.Of<IHttpClientFactory>();
            _mockApiSecrets = Mock.Of<IOptions<ApiSecrets>>();
            _mockApiSettings = Mock.Of<IOptions<ApiClientSettings>>();
            _mockLogger = Mock.Of<ILogger<SpotifyApiClient>>();
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_ValidAccessToken_ReturnsUserId()
        {
            // Arrange
            var accessToken = "valid-access-token";
            var expectedUserId = "spotify-user-id";
            var httpClient = new HttpClient(new TestHttpMessageHandler(async request =>
            {
                if (request.RequestUri.AbsoluteUri == "https://api.spotify.com/v1/me")
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(new SpotifyUser { Id = expectedUserId }))
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

            Mock.Get(_mockApiSecrets)
                .SetupGet(s => s.Value)
                .Returns(new ApiSecrets
                {
                    SpotifyClientId = "mock-client-id",
                    SpotifyClientSecret = "mock-client-secret",
                    SetlistFmApiKey = ""
                });

            Mock.Get(_mockHttpClientFactory)
                .Setup(factory => factory.CreateClient("SpotifyApiClient"))
                .Returns(httpClient);

            var spotifyApiClient = new SpotifyApiClient(
                _mockLogger ,
                _mockHttpClientFactory,
                _mockApiSecrets,
                _mockApiSettings);

            // Act
            var result = await spotifyApiClient.GetCurrentUserIdAsync(accessToken);

            // Assert
            Assert.Equal(expectedUserId, result);
        }

        [Fact]
        public async Task CreateNewSpotifyPlaylistAsync_ValidInputs_CreatesPlaylist()
        {
            // Arrange
            var accessToken = "valid-access-token";
            var userId = "spotify-user-id";
            var playlistName = "Test Playlist";
            var description = "Playlist description";

            var expectedPlaylistId = "new-playlist-id";
            var httpClient = new HttpClient(new TestHttpMessageHandler(async request =>
            {
                if (request.RequestUri.AbsoluteUri.Contains("users") && request.Method == HttpMethod.Post)
                {
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(new SpotifyPlaylist
                        {
                            PlaylistId = expectedPlaylistId,
                            PlaylistName = playlistName,
                            PlaylistDescription = description,
                            ExternalUrls = new ExternalUrls { Spotify = "" }
                        }
                            ))
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

            Mock.Get(_mockApiSecrets)
                .SetupGet(s => s.Value)
                .Returns(new ApiSecrets
                {
                    SpotifyClientId = "mock-client-id",
                    SpotifyClientSecret = "mock-client-secret",
                    SetlistFmApiKey = ""
                });

            Mock.Get(_mockApiSettings)
                .SetupGet(s => s.Value)
                .Returns(new ApiClientSettings
                {
                    SpotifyRedirectUri = "https://mock-redirect-uri",
                    SpotifyBaseUrl = "https://mock-spotify-base-url",
                    SpotifyAuthUri = "https://accounts.spotify.com/authorize",
                    SetlistFmBaseUrl = ""
                });

            Mock.Get(_mockHttpClientFactory)
                .Setup(factory => factory.CreateClient("SpotifyApiClient"))
                .Returns(httpClient);

            var spotifyApiClient = new SpotifyApiClient(
                _mockLogger,
                _mockHttpClientFactory,
                _mockApiSecrets,
                _mockApiSettings);

            var auth = new SpotifyAuth { AccessToken = accessToken };

            // Act
            var result = await spotifyApiClient.CreateNewSpotifyPlaylistAsync(auth, userId, playlistName, description);

            // Assert
            Assert.Equal(expectedPlaylistId, result.PlaylistId);
        }

        [Fact]
        public async Task FindSpotifyTracksFromSetlistAsync_ValidInputs_ReturnsTrackUris()
        {
            // Arrange
            var accessToken = "valid-access-token";
            var setlist = new Setlist
            {
                Artist = new Artist() { Name = "YourFaveBand" },
                Sets = new Sets
                {
                    Set =
                    [
                        new Set
                        {
                            Song =
                            [
                                new Song { Name = "Song1" },
                                new Song { Name = "Song2" }
                            ]
                        }
                    ]
                }
            };

            var expectedTrackUri = "spotify:track:123456";
            var httpClient = new HttpClient(new TestHttpMessageHandler(async request =>
            {
                if (request.RequestUri.AbsoluteUri.Contains("search") && request.Method == HttpMethod.Get)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(new SpotifySearchTracksResponse
                        {
                            Tracks = new Tracks { Items = [new Item { Uri = expectedTrackUri }] }
                        }))
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

            Mock.Get(_mockApiSettings)
                .SetupGet(s => s.Value)
                .Returns(new ApiClientSettings
                {
                    SpotifyRedirectUri = "https://mock-redirect-uri",
                    SpotifyBaseUrl = "https://mock-spotify-base-url",
                    SpotifyAuthUri = "https://accounts.spotify.com/authorize",
                    SetlistFmBaseUrl = ""
                });

            Mock.Get(_mockApiSecrets)
                .SetupGet(s => s.Value)
                .Returns(new ApiSecrets
                {
                    SpotifyClientId = "mock-client-id",
                    SpotifyClientSecret = "mock-client-secret",
                    SetlistFmApiKey = ""
                });

            Mock.Get(_mockHttpClientFactory)
                .Setup(factory => factory.CreateClient("SpotifyApiClient"))
                .Returns(httpClient);

            var spotifyApiClient = new SpotifyApiClient(
                _mockLogger,
                _mockHttpClientFactory,
                _mockApiSecrets,
                _mockApiSettings);

            var auth = new SpotifyAuth { AccessToken = accessToken };

            // Act
            (var result, var _) = await spotifyApiClient.FindSpotifyTracksFromSetlistAsync(auth, setlist);

            // Assert
            Assert.Contains(expectedTrackUri, result);
        }

        [Fact]
        public async Task UpdatePlaylistTracksAsync_ValidInputs_UpdatesTracks()
        {
            // Arrange
            var accessToken = "valid-access-token";
            var playlistId = "playlist-id";
            var trackUris = new[] { "spotify:track:123456", "spotify:track:789012" };

            var httpClient = new HttpClient(new TestHttpMessageHandler(async request =>
            {
                if (request.RequestUri.AbsoluteUri.Contains($"playlists/{playlistId}/tracks") && request.Method == HttpMethod.Post)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

            Mock.Get(_mockApiSettings)
            .SetupGet(s => s.Value)
            .Returns(new ApiClientSettings
            {
                SpotifyRedirectUri = "https://mock-redirect-uri",
                SpotifyBaseUrl = "https://mock-spotify-base-url",
                SpotifyAuthUri = "https://accounts.spotify.com/authorize",
                SetlistFmBaseUrl = ""
            });

            Mock.Get(_mockApiSecrets)
                .SetupGet(s => s.Value)
                .Returns(new ApiSecrets
                {
                    SpotifyClientId = "mock-client-id",
                    SpotifyClientSecret = "mock-client-secret",
                    SetlistFmApiKey = ""
                });

            Mock.Get(_mockHttpClientFactory)
                .Setup(factory => factory.CreateClient("SpotifyApiClient"))
                .Returns(httpClient);

            var spotifyApiClient = new SpotifyApiClient(
                _mockLogger,
                _mockHttpClientFactory,
                _mockApiSecrets,
                _mockApiSettings);

            var auth = new SpotifyAuth { AccessToken = accessToken };

            // Act
            var result = await spotifyApiClient.UpdatePlaylistTracksAsync(auth, playlistId, trackUris);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result);


        }

        // Helper class for mocking HTTP responses
        public class TestHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _sendAsyncFunc;

            public TestHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsyncFunc)
            {
                _sendAsyncFunc = sendAsyncFunc;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return await _sendAsyncFunc(request);
            }
        }
    }
}

