using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using SetlistToPlaylist.Api.Controllers;
using SetlistToPlaylist.Api.Models.SetlistFm;
using SetlistToPlaylist.Api.Models;
using SetlistToPlaylist.Api.Models.Spotify;
using SetlistToPlaylist.Api.RestApiClients.Interfaces;
using SetlistToPlaylist.Api.Services;
using SetlistToPlaylist.Api.Services.Interfaces;
using Xunit;

namespace SetlistToPlaylistTest.Api.Controllers
{
    public class SetlistToPlaylistControllerTests
    {
        private readonly Mock<ILogger<SetlistToPlaylistController>> _mockLogger;
        private readonly Mock<IPlaylistBuilder> _mockPlaylistBuilder;
        private readonly Mock<IAuthTokenFetcher> _mockTokenFetcher;
        private readonly SetlistToPlaylistController _controller;

        public SetlistToPlaylistControllerTests()
        {
            _mockLogger = new Mock<ILogger<SetlistToPlaylistController>>();
            _mockPlaylistBuilder = new Mock<IPlaylistBuilder>();
            _mockTokenFetcher = new Mock<IAuthTokenFetcher>();

            _controller = new SetlistToPlaylistController(
                _mockLogger.Object,
                _mockPlaylistBuilder.Object,
                _mockTokenFetcher.Object
            );

            // Mock HttpContext with session
            var sessionMock = new Mock<ISession>();
            var sessionStorage = new Dictionary<string, byte[]>();

            // Setup TryGetValue and Set behaviors for session mock
            sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback<string, byte[]>((key, value) => sessionStorage[key] = value);

            sessionMock.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
                .Returns((string key, out byte[] value) =>
                {
                    if (sessionStorage.TryGetValue(key, out var storedValue))
                    {
                        value = storedValue;
                        return true;
                    }
                    value = null;
                    return false;
                });

            // Create HttpContext and assign mocked session
            var httpContext = new DefaultHttpContext
            {
                Session = sessionMock.Object
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task GeneratePlaylistAsync_ValidRequest_ReturnsPlaylist()
        {
            // Arrange
            var setlistFmUrl = "https://setlist.fm/sample-url";
            var auth = new SpotifyAuth
            {
                AccessToken = "valid-token",
                ExpiresIn = 3600
            };
            auth.SetExpiryTime();
            var setlist = new Setlist
            {
                Artist = new Artist { Name = "The Foo Bar Figters" },
                EventDate = "01-02-2024"
            };
            var playlist = new SpotifyPlaylist
            {
                PlaylistId = "12345",
                PlaylistName = "Sample Playlist",
                PlaylistDescription = "Mock",
                ExternalUrls = new ExternalUrls { Spotify = "" }
            };

            // Save auth token to session
            _controller.HttpContext.Session.SetString("spotify_auth_token", JsonConvert.SerializeObject(auth));

            _mockPlaylistBuilder.Setup(pb => pb.GetSetlistAsync(setlistFmUrl))
                .ReturnsAsync(setlist);
            _mockPlaylistBuilder.Setup(pb => pb.CreatePlaylistAsync(
                It.IsAny<SpotifyAuth>(),
                It.IsAny<Setlist>()))
                .ReturnsAsync(playlist);

            // Act
            var result = await _controller.GeneratePlaylistAsync(setlistFmUrl) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var response = JsonConvert.DeserializeObject<GeneratePlaylistResponse>(result.Value.ToString());
            Assert.NotNull(response);
            Assert.Equal("Sample Playlist", response.Playlist?.PlaylistName);
        }

        [Fact]
        public async Task GeneratePlaylistAsync_Unauthorized_ReturnsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _controller.GeneratePlaylistAsync("https://setlist.fm/sample-url"));
        }
    }
}
