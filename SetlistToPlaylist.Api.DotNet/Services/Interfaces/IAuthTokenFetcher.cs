using SetlistToPlaylist.Api.Models.Spotify;

namespace SetlistToPlaylist.Api.Services.Interfaces;

public interface IAuthTokenFetcher
{
    public Task<SpotifyAuth> GetSpotifyAuthFromSession();
}

