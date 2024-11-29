using System.Net;
using SetlistToPlaylist.Api.Models.SetlistFm;
using SetlistToPlaylist.Api.Models.Spotify;

namespace SetlistToPlaylist.Api.RestApiClients.Interfaces
{
    public interface ISpotifyApiClient
    {
        public Task<string> GetCurrentUserIdAsync(string accessToken);
        public Task<SpotifyPlaylist> CreateNewSpotifyPlaylistAsync(SpotifyAuth auth, string userId, string playlistName, string description);
        public Task<(string[], string[])> FindSpotifyTracksFromSetlistAsync(SpotifyAuth auth, Setlist setlist);
        public Task<HttpStatusCode> UpdatePlaylistTracksAsync(SpotifyAuth auth, string playlistId, string[] trackUris);
    }
}
