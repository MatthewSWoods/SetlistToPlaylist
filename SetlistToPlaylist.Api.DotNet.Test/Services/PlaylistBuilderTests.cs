using Moq;
using SetlistToPlaylist.Api.Models.SetlistFm;
using SetlistToPlaylist.Api.Models.Spotify;
using SetlistToPlaylist.Api.RestApiClients.Interfaces;
using SetlistToPlaylist.Api.Services;
using Microsoft.Extensions.Logging;
using System.Net;

namespace SetlistToPlaylistTest.Api.Services
{
    public class PlayListBuilderTests
    {
        private readonly Mock<ILogger<PlayListBuilder>> _mockLogger;
        private readonly Mock<ISetlistFmApiClient> _mockSetlistFmApiClient;
        private readonly Mock<ISpotifyApiClient> _mockSpotifyApiClient;
        private readonly Mock<ISpotifyAuthClient> _mockSpotifyAuthClient;
        private readonly PlayListBuilder _playlistBuilder;

        public PlayListBuilderTests()
        {
            _mockLogger = new Mock<ILogger<PlayListBuilder>>();
            _mockSetlistFmApiClient = new Mock<ISetlistFmApiClient>();
            _mockSpotifyApiClient = new Mock<ISpotifyApiClient>();
            _mockSpotifyAuthClient = new Mock<ISpotifyAuthClient>();

            _playlistBuilder = new PlayListBuilder(
                _mockLogger.Object,
                _mockSetlistFmApiClient.Object,
                _mockSpotifyApiClient.Object,
                _mockSpotifyAuthClient.Object
            );
        }

        [Fact]
        public async Task GetSetlistAsync_ValidUrl_ReturnsSetlist()
        {
            // Arrange
            var setlistFmUrl = "https://setlist.fm/sample-url";
            var expectedSetlist = new Setlist { Artist = new Artist { Name = "Foo Fighters" } };

            _mockSetlistFmApiClient.Setup(client => client.GetSetlistFromUrlAsync(setlistFmUrl))
                .ReturnsAsync(expectedSetlist);

            // Act
            var result = await _playlistBuilder.GetSetlistAsync(setlistFmUrl);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Foo Fighters", result.Artist?.Name);
            _mockSetlistFmApiClient.Verify(client => client.GetSetlistFromUrlAsync(setlistFmUrl), Times.Once);
        }

        [Fact]
        public async Task CreatePlaylistAsync_ValidSetlist_CreatesPlaylist()
        {
            // Arrange
            var auth = new SpotifyAuth { AccessToken = "valid-token" };
            var setlist = new Setlist
            {
                Artist = new Artist { Name = "Foo Bar Fighters" },
                EventDate = "01-02-2024",
                Venue = new Venue { Name = "Madison Square Garden", City = new City { Name = "New York" } }
            };

            var spotifyUserId = "user-id";
            var expectedPlaylist = new SpotifyPlaylist
            {
                PlaylistId = "playlist-id",
                PlaylistName = "2024 Foo Bar Fighters",
                PlaylistDescription = "Live @ Madison Square Garden, New York on 01-02-2024",
                ExternalUrls = new ExternalUrls { Spotify = "" }
            };

            _mockSpotifyApiClient.Setup(client => client.GetCurrentUserIdAsync(
                    It.IsAny<string>()))
                .ReturnsAsync(spotifyUserId);
            _mockSpotifyApiClient.Setup(client => client.CreateNewSpotifyPlaylistAsync(
                    It.IsAny<SpotifyAuth>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(expectedPlaylist);

            // Act
            var result = await _playlistBuilder.CreatePlaylistAsync(auth, setlist);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("2024 Foo Bar Fighters", result.PlaylistName);
            _mockSpotifyApiClient.Verify(client => client.CreateNewSpotifyPlaylistAsync(auth, spotifyUserId, expectedPlaylist.PlaylistName, expectedPlaylist.PlaylistDescription), Times.Once);
        }

        [Fact]
        public async Task PopulatePlaylistAsync_ValidData_PopulatesPlaylist()
        {
            // Arrange
            var auth = new SpotifyAuth { AccessToken = "valid-token" };
            var setlist = new Setlist();
            var playlistId = "playlist-id";
            var spotifyTracks = new string[] { "spotify:track:123", "spotify:track:456" };
            var failedTracks = new string[] { "track1", "track2" };

            _mockSpotifyApiClient.Setup(client => client.FindSpotifyTracksFromSetlistAsync(
                    It.IsAny<SpotifyAuth>(),
                    It.IsAny<Setlist>()))
                .ReturnsAsync((spotifyTracks, failedTracks));
            _mockSpotifyApiClient.Setup(client => client.UpdatePlaylistTracksAsync(
                    It.IsAny<SpotifyAuth>(),
                    It.IsAny<string>(),
                    It.IsAny<string[]>()))
                .ReturnsAsync(HttpStatusCode.OK);

            // Act
            var result = await _playlistBuilder.PopulatePlaylistAsync(auth, playlistId, setlist);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(spotifyTracks, result.Item1);
            Assert.Equal(failedTracks, result.Item2);
            _mockSpotifyApiClient.Verify(client => client.UpdatePlaylistTracksAsync(auth, playlistId, spotifyTracks), Times.Once);
        }

        [Fact]
        public async Task CreatePlaylistAsync_NullEventDate_ThrowsException()
        {
            // Arrange
            var auth = new SpotifyAuth { AccessToken = "valid-token" };
            var setlist = new Setlist
            {
                Artist = new Artist { Name = "Foo Fighters" },
                EventDate = null
            };

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => _playlistBuilder.CreatePlaylistAsync(auth, setlist));
        }

        [Fact]
        public async Task CreatePlaylistAsync_NullAccessToken_ThrowsException()
        {
            // Arrange
            var auth = new SpotifyAuth { AccessToken = null };
            var setlist = new Setlist
            {
                Artist = new Artist { Name = "Foo Fighters" },
                EventDate = "01-02-2024"
            };

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => _playlistBuilder.CreatePlaylistAsync(auth, setlist));
        }

        [Fact]
        public async Task PopulatePlaylistAsync_FailedTracks_ReturnsFailedTracks()
        {
            // Arrange
            var auth = new SpotifyAuth { AccessToken = "valid-token" };
            var setlist = new Setlist();
            var playlistId = "playlist-id";
            var spotifyTracks = new string[] { "spotify:track:123", "spotify:track:456" };
            var failedTracks = new string[] { "track1", "track2" };

            _mockSpotifyApiClient.Setup(client => client.FindSpotifyTracksFromSetlistAsync(
                    It.IsAny<SpotifyAuth>(),
                    It.IsAny<Setlist>()))
                .ReturnsAsync((spotifyTracks, failedTracks));
            _mockSpotifyApiClient.Setup(client => client.UpdatePlaylistTracksAsync(
                    It.IsAny<SpotifyAuth>(),
                    It.IsAny<string>(),
                    It.IsAny<string[]>()))
                .ReturnsAsync(HttpStatusCode.OK);

            // Act
            var result = await _playlistBuilder.PopulatePlaylistAsync(auth, playlistId, setlist);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(failedTracks, result.Item2);
        }
    }
}
