using System.Globalization;
using SetlistToPlaylist.Api.Models.SetlistFm;
using SetlistToPlaylist.Api.Models.Spotify;
using SetlistToPlaylist.Api.RestApiClients.Interfaces;
using SetlistToPlaylist.Api.Services.Interfaces;

namespace SetlistToPlaylist.Api.Services
{
    public class PlayListBuilder : IPlaylistBuilder
    {
        private readonly ILogger<PlayListBuilder> _logger;
        private readonly ISetlistFmApiClient _setlistFmApiClient;
        private readonly ISpotifyApiClient _spotifyApiClient;
        private readonly ISpotifyAuthClient _spotifyAuthClient;
        public PlayListBuilder(
            ILogger<PlayListBuilder> logger,
            ISetlistFmApiClient setlistFmApiClient,
            ISpotifyApiClient spotifyApiClient,
            ISpotifyAuthClient spotifyAuthClient
            )
        {
            _logger = logger;
            _setlistFmApiClient = setlistFmApiClient;
            _spotifyApiClient = spotifyApiClient;
            _spotifyAuthClient = spotifyAuthClient;
        }

        public async Task<Setlist> GetSetlistAsync(string setlistFmUrl)
        {
            _logger.LogInformation($"Generating setlist from: {setlistFmUrl}");
            return await _setlistFmApiClient.GetSetlistFromUrlAsync(setlistFmUrl);
        }

        public async Task<SpotifyPlaylist> CreatePlaylistAsync(SpotifyAuth auth, Setlist setlist)
        {
            _logger.LogInformation("Creating Spotify playlist");
            var playlistDate = DateTime.ParseExact(setlist.EventDate ?? throw new NullReferenceException("Event Date is null"), "d-M-yyyy", CultureInfo.InvariantCulture);
            var playlistName = playlistDate.Year.ToString() + " " + setlist.Artist?.Name;
            var playlistDescription = $"Live @ {setlist.Venue?.Name}, {setlist.Venue?.City?.Name} on {playlistDate:dd-MM-yyyy}";

            var spotifyUserId = await _spotifyApiClient.GetCurrentUserIdAsync(auth.AccessToken ?? throw new NullReferenceException("Access token cannot be null here"));
            
            return await _spotifyApiClient.CreateNewSpotifyPlaylistAsync(auth, spotifyUserId, playlistName, playlistDescription);
        }

        public async Task<(string[], string[])> PopulatePlaylistAsync(SpotifyAuth auth, string  playlistId, Setlist setlist) 
        {
            _logger.LogInformation("populating Spotify playlist");
            var (spotifyTracks, failedTracks) = await _spotifyApiClient.FindSpotifyTracksFromSetlistAsync(auth, setlist);
            await _spotifyApiClient.UpdatePlaylistTracksAsync(auth, playlistId, spotifyTracks);
            
            return (spotifyTracks, failedTracks);
        }
    }
}
