using SetlistToPlaylist.Api.Models.SetlistFm;
using SetlistToPlaylist.Api.Models.Spotify;

namespace SetlistToPlaylist.Api.Services.Interfaces
{
    public interface IPlaylistBuilder
    {
        public Task<Setlist> GetSetlistAsync(string setlistFmUrl);
        public Task<SpotifyPlaylist> CreatePlaylistAsync(SpotifyAuth auth, Setlist setlist);
        public Task<(string[], string[])> PopulatePlaylistAsync(SpotifyAuth auth, string playlistId, Setlist setlist);
    }
}
