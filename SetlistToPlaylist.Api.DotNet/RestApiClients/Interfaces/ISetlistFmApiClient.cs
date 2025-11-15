using SetlistToPlaylist.Api.Models.SetlistFm;

namespace SetlistToPlaylist.Api.RestApiClients.Interfaces
{
    public interface ISetlistFmApiClient
    {
        public Task<Setlist> GetSetlistFromUrlAsync(string url);
    }
}
