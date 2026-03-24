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

    /// <summary>Set by the frontend after claiming a transfer token. Added as X-Client-Key on all requests.</summary>
    public string? ClientKey { get; set; }

    public async Task<string?> ClaimAsync(string transferToken, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "auth/claim",
            new { transferToken },
            JsonOptions, ct);

        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<ClaimResponseDto>(JsonOptions, ct);
        return result?.ClientKey;
    }

    public async Task<GenerateResult> GeneratePlaylistAsync(
        string setlistUrl, string connectionId, bool isPublic, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/setlist/generate")
        {
            Content = JsonContent.Create(new { setlistUrl, connectionId, isPublic }, options: JsonOptions)
        };
        AddClientKeyHeader(request);

        var response = await _httpClient.SendAsync(request, ct);

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
            var request = new HttpRequestMessage(HttpMethod.Get, "auth/status");
            AddClientKeyHeader(request);

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<AuthStatusDto>(JsonOptions, ct);
            return result?.Authenticated ?? false;
        }
        catch
        {
            return false;
        }
    }

    private void AddClientKeyHeader(HttpRequestMessage request)
    {
        if (ClientKey is not null)
            request.Headers.TryAddWithoutValidation("X-Client-Key", ClientKey);
    }

    private sealed record GeneratePlaylistStartedDto(string? PlaylistId, string? PlaylistUrl);
    private sealed record AuthStatusDto([property: JsonPropertyName("authenticated")] bool Authenticated);
    private sealed record ClaimResponseDto([property: JsonPropertyName("clientKey")] string? ClientKey);
}

public sealed class GenerateResult
{
    public bool NeedsAuth { get; init; }
    public string? PlaylistId { get; init; }
    public string? PlaylistUrl { get; init; }
    public string? Error { get; init; }
}
