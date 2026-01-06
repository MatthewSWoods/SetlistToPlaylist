using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

namespace SetlistToPlaylist.ApiService.Contracts.SetlistFm;

public sealed record GetSetlistResponse
{
    public SetlistDto? setlist { get; set; }
}
