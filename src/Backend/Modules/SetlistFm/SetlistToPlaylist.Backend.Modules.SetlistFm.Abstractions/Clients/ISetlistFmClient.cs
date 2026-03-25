using FluentResults;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.Clients;

public interface ISetlistFmClient
{
    Task<Result<SetlistDto>> GetSetlistByIdAsync(string setlistId, CancellationToken ct = default);
}
