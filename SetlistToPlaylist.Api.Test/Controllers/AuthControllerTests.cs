using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using SetlistToPlaylist.Api.Controllers;
using SetlistToPlaylist.Api.Models.Spotify;
using SetlistToPlaylist.Api.RestApiClients.Interfaces;
using SetlistToPlaylist.Api.Settings;
using SetlistToPlaylist.Api.Test.Helpers;
using System.Threading.Tasks;
using Xunit;

namespace SetlistToPlaylistTest.Api.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly Mock<ISpotifyAuthClient> _mockAuthClient;
        private readonly Mock<IOptions<FrontEndClientSettings>> _mockSettings;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockLogger = new Mock<ILogger<AuthController>>();
            _mockAuthClient = new Mock<ISpotifyAuthClient>();
            _mockSettings = new Mock<IOptions<FrontEndClientSettings>>();

            _mockSettings.Setup(s => s.Value).Returns(new FrontEndClientSettings { BaseUrl = "https://test.com" });

            _controller = new AuthController(
                _mockLogger.Object,
                _mockAuthClient.Object,
                _mockSettings.Object
            );

            // Mock HttpContext with session
            var httpContext = new DefaultHttpContext
            {
                Session = SessionMockHelper.CreateMockSession()
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public void Login_ReturnsRedirectToAuthUrl()
        {
            // Arrange
            var authUrlStateless = "https://auth.example.com";
            var authUrl = "https://auth.example.com?state=12345";
            var state = "12345";

            _mockAuthClient.Setup(c => c.CreateOAuthRequestUrl()).Returns(authUrlStateless);
            _mockAuthClient.Setup(c => c.AddStateToOAuthRequestUrl(authUrlStateless))
                .Returns((authUrl, state));

            // Act
            var result = _controller.Login() as RedirectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(authUrl, result.Url);
            Assert.Equal(state, _controller.HttpContext.Session.GetString("spotify_auth_state"));
        }

        [Fact]
        public async Task Callback_InvalidCode_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Callback("", "12345") as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Authorization code not found in callback", result.Value);
        }

        [Fact]
        public async Task Callback_InvalidState_ReturnsBadRequest()
        {
            // Arrange
            _controller.HttpContext.Session.SetString("spotify_auth_state", "67890");

            // Act
            var result = await _controller.Callback("valid-code", "12345") as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Invalid state in sessionState or callbackState", result.Value);
        }

        [Fact]
        public async Task Callback_ValidRequest_SavesTokenAndRedirects()
        {
            // Arrange
            var token = new SpotifyAuth { AccessToken = "access-token" };
            _controller.HttpContext.Session.SetString("spotify_auth_state", "12345");

            _mockAuthClient.Setup(c => c.GetTokenAsync("valid-code"))
                .ReturnsAsync(token);

            // Act
            var result = await _controller.Callback("valid-code", "12345") as RedirectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("https://test.com", result.Url);
            Assert.Equal(
                JsonConvert.SerializeObject(token),
                _controller.HttpContext.Session.GetString("spotify_auth_token")
            );
        }

        [Fact]
        public void Logout_RemovesTokenAndRedirects()
        {
            // Arrange
            _controller.HttpContext.Session.SetString("spotify_auth_token", "some-token");

            // Act
            var result = _controller.Logout() as RedirectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("https://test.com", result.Url);
            Assert.Null(_controller.HttpContext.Session.GetString("spotify_auth_token"));
        }

        [Fact]
        public void IsLoggedIn_WhenTokenExists_ReturnsTrue()
        {
            // Arrange
            _controller.HttpContext.Session.SetString("spotify_auth_token", "some-token");

            // Act
            var result = _controller.IsLoggedIn() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.True((bool)result.Value);
        }

        [Fact]
        public void IsLoggedIn_WhenNoTokenExists_ReturnsFalse()
        {
            // Act
            var result = _controller.IsLoggedIn() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.False((bool)result.Value);
        }
    }
}

    