using System.Net;
using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Logging;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.Clients;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Clients;

public sealed class SetlistFmClient : ISetlistFmClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SetlistFmClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SetlistFmClient(HttpClient httpClient, ILogger<SetlistFmClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<SetlistDto>> GetSetlistByIdAsync(string setlistId, CancellationToken ct = default)
    {
        var url = $"setlist/{setlistId}";
        _logger.LogInformation("Fetching setlist {SetlistId} from Setlist.fm", setlistId);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP error fetching setlist {SetlistId}", setlistId);
            return Result.Fail($"Failed to contact Setlist.fm: {ex.Message}");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result.Fail($"Setlist '{setlistId}' not found on Setlist.fm");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Setlist.fm returned {StatusCode} for setlist {SetlistId}", response.StatusCode, setlistId);
            return Result.Fail($"Setlist.fm returned {(int)response.StatusCode}");
        }

        var stream = await response.Content.ReadAsStreamAsync(ct);
        var setlist = await JsonSerializer.DeserializeAsync<SetlistDto>(stream, JsonOptions, ct);

        if (setlist is null)
            return Result.Fail("Failed to deserialize setlist response");

        return Result.Ok(setlist);
    }
}
