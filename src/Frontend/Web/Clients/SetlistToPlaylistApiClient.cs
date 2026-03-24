using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Web.Clients;

public sealed class SetlistToPlaylistApiClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SetlistToPlaylistApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GenerateResult> GeneratePlaylistAsync(
        string setlistUrl, string connectionId, bool isPublic, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/v1/setlist/generate",
            new { setlistUrl, connectionId, isPublic },
            JsonOptions, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return new GenerateResult { NeedsAuth = true };

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            return new GenerateResult { Error = $"({(int)response.StatusCode}) {error}" };
        }

        var result = await response.Content.ReadFromJsonAsync<GeneratePlaylistStartedDto>(JsonOptions, ct);
        return new GenerateResult
        {
            PlaylistId = result?.PlaylistId,
            PlaylistUrl = result?.PlaylistUrl
        };
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<AuthStatusDto>("auth/status", JsonOptions, ct);
            return response?.Authenticated ?? false;
        }
        catch
        {
            return false;
        }
    }

    private sealed record GeneratePlaylistStartedDto(string? PlaylistId, string? PlaylistUrl);
    private sealed record AuthStatusDto([property: JsonPropertyName("authenticated")] bool Authenticated);
}

public sealed class GenerateResult
{
    public bool NeedsAuth { get; init; }
    public string? PlaylistId { get; init; }
    public string? PlaylistUrl { get; init; }
    public string? Error { get; init; }
}
