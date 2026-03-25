using System.Net.Http.Headers;
using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Clients;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.Spotify.Extensions;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Clients;

public sealed class SpotifyAuthClient : ISpotifyAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SpotifyAuthClient> _logger;
    private readonly TimeProvider _timeProvider;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SpotifyAuthClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SpotifyAuthClient> logger,
        TimeProvider timeProvider)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthDto>> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken ct = default)
    {
        var clientId = _configuration["Spotify:ClientId"]
            ?? throw new InvalidOperationException("Spotify:ClientId is not configured");
        var callbackUrl = _configuration["Spotify:CallbackUrl"]
            ?? throw new InvalidOperationException("Spotify:CallbackUrl is not configured");

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = callbackUrl,
            ["client_id"] = clientId,
            ["code_verifier"] = codeVerifier
        });

        return await PostTokenAsync(body, ct);
    }

    public async Task<Result<AuthDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var clientId = _configuration["Spotify:ClientId"]
            ?? throw new InvalidOperationException("Spotify:ClientId is not configured");

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId
        });

        return await PostTokenAsync(body, ct);
    }

    private async Task<Result<AuthDto>> PostTokenAsync(FormUrlEncodedContent body, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/token") { Content = body };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error contacting Spotify token endpoint");
            return Result.Fail($"Failed to contact Spotify auth: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Spotify token endpoint returned {StatusCode}: {Error}", response.StatusCode, error);
            return Result.Fail($"Spotify auth failed ({(int)response.StatusCode}): {error}");
        }

        var stream = await response.Content.ReadAsStreamAsync(ct);
        var authDto = await JsonSerializer.DeserializeAsync<AuthDto>(stream, JsonOptions, ct);

        if (authDto is null)
            return Result.Fail("Failed to deserialize Spotify token response");

        authDto.SetExpiryTime(_timeProvider);
        return Result.Ok(authDto);
    }
}
