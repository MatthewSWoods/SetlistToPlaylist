using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using SetlistToPlaylist.Api.Models.SetlistFm;
using SetlistToPlaylist.Api.RestApiClients;
using SetlistToPlaylist.Api.Settings;

namespace SetlistToPlaylistTest.Api.RestApiClients
{
    public class SetlistFmApiClientTests
    {
        private readonly IHttpClientFactory _mockHttpClientFactory;
        private readonly IOptions<ApiClientSettings> _mockApiSettings;
        private readonly IOptions<ApiSecrets> _mockApiSecrets;
        private readonly ILogger<SetlistFmApiClient> _mockLogger;

        public SetlistFmApiClientTests()
        {
            _mockHttpClientFactory = Mock.Of<IHttpClientFactory>();
            _mockApiSecrets = Mock.Of<IOptions<ApiSecrets>>();
            _mockApiSettings = Mock.Of<IOptions<ApiClientSettings>>();
            _mockLogger = Mock.Of<ILogger<SetlistFmApiClient>>();
        }

        [Fact]
        public async Task GetSetlistFromUrlAsync_ValidUrl_ReturnsSetlist()
        {
            // Arrange
            var url = "https://www.setlist.fm/setlist/foo-bar-2023-abc123.html";
            var setlistId = "abc123";
            var expectedSetlist = new Setlist { Id = setlistId };

            var httpClient = new HttpClient(new TestHttpMessageHandler(async request =>
            {
                if (request.RequestUri.AbsoluteUri == "https://api.setlist.fm/setlist/abc123")
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(expectedSetlist))
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

            Mock.Get(_mockHttpClientFactory)
                .Setup(factory => factory.CreateClient("SetlistFmApiClient"))
                .Returns(httpClient);

            Mock.Get(_mockApiSettings)
                .SetupGet(settings => settings.Value)
                .Returns(new ApiClientSettings
                    {
                        SetlistFmBaseUrl = "https://api.setlist.fm",
                        SpotifyAuthUri = "",
                        SpotifyRedirectUri = "",
                        SpotifyBaseUrl = ""
                    });

            Mock.Get(_mockApiSecrets)
                .SetupGet(secrets => secrets.Value)
                .Returns(new ApiSecrets
                    {
                        SetlistFmApiKey = "mock-api-key",
                        SpotifyClientId = "",
                        SpotifyClientSecret = ""
                    });

            var setlistFmApiClient = new SetlistFmApiClient(
                _mockLogger,
                _mockHttpClientFactory,
                _mockApiSettings,
                _mockApiSecrets);

            // Act
            var result = await setlistFmApiClient.GetSetlistFromUrlAsync(url);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedSetlist.Id, result.Id);
        }

        [Fact]
        public async Task GetSetlistFromUrlAsync_InvalidUrl_ThrowsException()
        {
            // Arrange
            var url = "https://www.setlist.fm/setlist/foo-bar-2023-invalid.html";

            var httpClient = new HttpClient(new TestHttpMessageHandler(async request =>
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

            Mock.Get(_mockHttpClientFactory)
                .Setup(factory => factory.CreateClient("SetlistFmApiClient"))
                .Returns(httpClient);

            Mock.Get(_mockApiSettings)
                .SetupGet(settings => settings.Value)
                .Returns(new ApiClientSettings
                {
                    SetlistFmBaseUrl = "https://api.setlist.fm",
                    SpotifyAuthUri = "",
                    SpotifyRedirectUri = "",
                    SpotifyBaseUrl = ""
                });

            Mock.Get(_mockApiSecrets)
                .SetupGet(secrets => secrets.Value)
                .Returns(new ApiSecrets
                {
                    SetlistFmApiKey = "mock-api-key",
                    SpotifyClientId = "",
                    SpotifyClientSecret = ""
                });

            var setlistFmApiClient = new SetlistFmApiClient(
                _mockLogger,
                _mockHttpClientFactory,
                _mockApiSettings,
                _mockApiSecrets);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => setlistFmApiClient.GetSetlistFromUrlAsync(url));
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


