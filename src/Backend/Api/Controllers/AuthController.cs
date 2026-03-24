using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using SetlistToPlaylist.ApiService.Contracts.Core;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Clients;

namespace SetlistToPlaylist.ApiService.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private const string PkceStateKeyPrefix = "pkce_state:";
    private const string PkceVerifierKeyPrefix = "pkce_verifier:";
    private const string SpotifyAuthKeyPrefix = "spotify_auth:";
    private const string TransferTokenKeyPrefix = "transfer_token:";

    private readonly ISpotifyAuthClient _authClient;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly TimeProvider _timeProvider;

    public AuthController(
        ISpotifyAuthClient authClient,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        TimeProvider timeProvider)
    {
        _authClient = authClient;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    [HttpGet("login")]
    public async Task<IActionResult> Login(CancellationToken ct)
    {
        await HttpContext.Session.LoadAsync(ct);
        var sessionId = HttpContext.Session.Id;

        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = ComputeCodeChallenge(codeVerifier);
        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
            .Replace("+", "-").Replace("/", "_").Replace("=", string.Empty);

        var expiry = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
        await _cache.SetStringAsync($"{PkceStateKeyPrefix}{sessionId}", state, expiry, ct);
        await _cache.SetStringAsync($"{PkceVerifierKeyPrefix}{sessionId}", codeVerifier, expiry, ct);

        var clientId = _configuration["Spotify:ClientId"]
            ?? throw new InvalidOperationException("Spotify:ClientId is not configured");
        var callbackUrl = _configuration["Spotify:CallbackUrl"]
            ?? throw new InvalidOperationException("Spotify:CallbackUrl is not configured");

        var scopes = "playlist-modify-private playlist-modify-public user-read-private";
        var spotifyAuthUrl =
            $"https://accounts.spotify.com/authorize" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}" +
            $"&code_challenge_method=S256" +
            $"&code_challenge={codeChallenge}" +
            $"&state={Uri.EscapeDataString(state)}" +
            $"&scope={Uri.EscapeDataString(scopes)}";

        _logger.LogInformation("Initiating Spotify PKCE login for session {SessionId}", sessionId);
        return Redirect(spotifyAuthUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("Spotify auth callback returned error: {Error}", error);
            return Redirect("/?auth_error=access_denied");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return BadRequest("Missing code or state parameter");

        await HttpContext.Session.LoadAsync(ct);
        var sessionId = HttpContext.Session.Id;

        var storedState = await _cache.GetStringAsync($"{PkceStateKeyPrefix}{sessionId}", ct);
        if (storedState != state)
        {
            _logger.LogWarning("OAuth state mismatch for session {SessionId}", sessionId);
            return BadRequest("Invalid OAuth state — possible CSRF attempt");
        }

        var codeVerifier = await _cache.GetStringAsync($"{PkceVerifierKeyPrefix}{sessionId}", ct);
        if (string.IsNullOrEmpty(codeVerifier))
        {
            _logger.LogWarning("PKCE verifier not found for session {SessionId}", sessionId);
            return BadRequest("PKCE verifier not found — please try logging in again");
        }

        // Clean up PKCE entries
        await _cache.RemoveAsync($"{PkceStateKeyPrefix}{sessionId}", ct);
        await _cache.RemoveAsync($"{PkceVerifierKeyPrefix}{sessionId}", ct);

        var tokenResult = await _authClient.ExchangeCodeAsync(code, codeVerifier, ct);
        if (tokenResult.IsFailed)
        {
            _logger.LogError("Token exchange failed for session {SessionId}: {Error}",
                sessionId, tokenResult.Errors[0].Message);
            return Redirect("/?auth_error=token_exchange_failed");
        }

        var auth = tokenResult.Value;
        var json = System.Text.Json.JsonSerializer.Serialize(auth);
        var expiry = auth.ExpiryTime.HasValue
            ? auth.ExpiryTime.Value - _timeProvider.GetUtcNow().UtcDateTime
            : TimeSpan.FromHours(1);

        await _cache.SetStringAsync(
            $"{SpotifyAuthKeyPrefix}{sessionId}", json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry },
            ct);

        // Ensure session cookie is committed
        HttpContext.Session.SetString("authenticated", "true");

        // Generate a short-lived one-time transfer token so the frontend can claim the clientKey
        // without relying on the browser's session cookie crossing origins.
        var transferToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", string.Empty);
        var transferExpiry = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };
        await _cache.SetStringAsync(
            $"{TransferTokenKeyPrefix}{transferToken}", sessionId, transferExpiry, ct);

        _logger.LogInformation("Spotify authentication successful for session {SessionId}", sessionId);

        var frontendUrl = _configuration["Frontend:Url"] ?? "https://localhost:5002";
        return Redirect($"{frontendUrl}/?at={Uri.EscapeDataString(transferToken)}");
    }

    [HttpPost("claim")]
    public async Task<IActionResult> Claim([FromBody] ClaimRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.TransferToken))
            return BadRequest("Transfer token is required");

        var clientKey = await _cache.GetStringAsync(
            $"{TransferTokenKeyPrefix}{request.TransferToken}", ct);
        if (clientKey is null)
        {
            _logger.LogWarning("Transfer token not found or expired");
            return BadRequest("Invalid or expired transfer token");
        }

        await _cache.RemoveAsync($"{TransferTokenKeyPrefix}{request.TransferToken}", ct);
        return Ok(new { clientKey });
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        string clientKey;
        var headerKey = HttpContext.Request.Headers["X-Client-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerKey))
        {
            clientKey = headerKey;
        }
        else
        {
            await HttpContext.Session.LoadAsync(ct);
            clientKey = HttpContext.Session.Id;
        }

        var token = await _cache.GetStringAsync($"{SpotifyAuthKeyPrefix}{clientKey}", ct);
        return Ok(new { authenticated = token is not null });
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", string.Empty);
    }

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .Replace("+", "-").Replace("/", "_").Replace("=", string.Empty);
    }
}
