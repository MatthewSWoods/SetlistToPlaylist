using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using SetlistToPlaylist.Api.Models.Spotify;
using SetlistToPlaylist.Api.RestApiClients;
using SetlistToPlaylist.Api.Settings;


namespace SetlistToPlaylistTest.Api.RestApiClients
{
    public class SpotifyAuthClientTests
    {
        private readonly IHttpClientFactory _mockHttpClientFactory;
        private readonly IOptions<ApiSecrets> _mockApiSecrets;
        private readonly IOptions<ApiClientSettings> _mockApiSettings;
        private readonly ILogger<SpotifyAuthClient> _mockLogger;

        public SpotifyAuthClientTests()
        {
            _mockHttpClientFactory = Mock.Of<IHttpClientFactory>();
            _mockApiSecrets = Mock.Of<IOptions<ApiSecrets>>();
            _mockApiSettings = Mock.Of<IOptions<ApiClientSettings>>();
            _mockLogger = Mock.Of<ILogger<SpotifyAuthClient>>();
        }

        [Fact]
        public async Task GetTokenAsync_ValidCode_ReturnsSpotifyAuth()
        {
            // Arrange
            var code = "valid-code";
            var expectedAuth = new SpotifyAuth
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresIn = 3600,
                TokenType = "Bearer"
            };

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
                    SpotifyAuthUri = "https://accounts.spotify.com/authorize",
                    SpotifyBaseUrl = "",
                    SetlistFmBaseUrl = ""
                });

            var httpClient = new HttpClient(new TestHttpMessageHandler(async request =>
            {
                if (request.RequestUri.AbsoluteUri == "https://accounts.spotify.com/api/token")
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(expectedAuth))
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

            Mock.Get(_mockHttpClientFactory)
                .Setup(factory => factory.CreateClient("SpotifyAuthClient"))
                .Returns(httpClient);

            var spotifyAuthClient = new SpotifyAuthClient(
                _mockLogger,
                _mockHttpClientFactory,
                _mockApiSecrets,
                _mockApiSettings);

            // Act
            var result = await spotifyAuthClient.GetTokenAsync(code);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedAuth.AccessToken, result.AccessToken);
            Assert.Equal(expectedAuth.RefreshToken, result.RefreshToken);
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidSpotifyAuth_ReturnsRefreshedSpotifyAuth()
        {
            // Arrange
            var spotifyAuth = new SpotifyAuth
            {
                RefreshToken = "refresh-token"
            };

            Mock.Get(_mockApiSecrets)
                .SetupGet(s => s.Value)
                .Returns(new ApiSecrets
                {
                    SpotifyClientId = "mock-client-id",
                    SpotifyClientSecret = "mock-client-secret",
                    SetlistFmApiKey = ""
                });

            var expectedAuth = new SpotifyAuth
            {
                AccessToken = "new-access-token",
                RefreshToken = "new-refresh-token",
                ExpiresIn = 3600,
                TokenType = "Bearer"
            };

            var httpClient = new HttpClient(new TestHttpMessageHandler(async request =>
            {
                if (request.RequestUri.AbsoluteUri == "https://accounts.spotify.com/api/token")
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(expectedAuth))
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

            Mock.Get(_mockHttpClientFactory)
                .Setup(factory => factory.CreateClient("SpotifyAuthClient"))
                .Returns(httpClient);

            var spotifyAuthClient = new SpotifyAuthClient(
                _mockLogger,
                _mockHttpClientFactory,
                _mockApiSecrets,
                _mockApiSettings);

            // Act
            var result = await spotifyAuthClient.RefreshTokenAsync(spotifyAuth);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedAuth.AccessToken, result.AccessToken);
            Assert.Equal(expectedAuth.RefreshToken, result.RefreshToken);
        }

        [Fact]
        public void CreateOAuthRequestUrl_ReturnsValidUrl()
        {
            // Arrange
            var expectedUrl = "https://accounts.spotify.com/authorize?client_id=mock-client-id&response_type=code&redirect_uri=https%3A%2F%2Fmock-redirect-uri&show_dialog=true&scope=user-read-private%20user-read-email%20playlist-modify-private%20playlist-modify-public%20playlist-read-private%20playlist-read-collaborative%20user-library-modify%20user-library-read";

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
                        SpotifyAuthUri = "https://accounts.spotify.com/authorize",
                        SpotifyBaseUrl = "",
                        SetlistFmBaseUrl = ""
                    });

            var spotifyAuthClient = new SpotifyAuthClient(
                _mockLogger,
                _mockHttpClientFactory,
                _mockApiSecrets,
                _mockApiSettings);

            // Act
            var result = spotifyAuthClient.CreateOAuthRequestUrl();

            // Assert
            Assert.Equal(expectedUrl, result);
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
