using SetlistToPlaylist.Api.Models.Spotify;

namespace SetlistToPlaylist.Api.RestApiClients.Interfaces
{
    public interface ISpotifyAuthClient
    {
        public Task<SpotifyAuth> GetTokenAsync(string code);
        public Task<SpotifyAuth> RefreshTokenAsync(SpotifyAuth spotifyAuth);
        public string CreateOAuthRequestUrl();
        public (string, string) AddStateToOAuthRequestUrl(string url);
    }
}
