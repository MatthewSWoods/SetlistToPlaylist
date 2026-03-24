using FluentResults;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.Services;

public interface ISetlistFmService
{
    Task<Result<SetlistDto>> GetSetlistAsync(string setlistFmUrl, CancellationToken ct = default);
}
