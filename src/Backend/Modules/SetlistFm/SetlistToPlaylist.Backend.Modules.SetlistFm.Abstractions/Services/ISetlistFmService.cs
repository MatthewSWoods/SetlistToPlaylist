namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.Services;

public interface ISetlistFmService
{
    public Task GetSetlistAsync(string url);
}
