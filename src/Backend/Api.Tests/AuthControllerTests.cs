using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SetlistToPlaylist.ApiService.Contracts.Core;
using SetlistToPlaylist.ApiService.Controllers;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.Clients;

namespace SetlistToPlaylist.Backend.ApiService.Tests;

/// <summary>
/// Tests for the AuthController, particularly focusing on the logout functionality.
/// </summary>
public sealed class AuthControllerTests
{
    private const string SessionId = "test-session-id";
    private const string ClientKey = "test-client-key";
    private const string SpotifyAuthKeyPrefix = "spotify_auth:";

    private readonly ISpotifyAuthClient _authClient = Substitute.For<ISpotifyAuthClient>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly IConfiguration _configuration;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        var configDict = new Dictionary<string, string?>
        {
            { "Spotify:ClientId", "test-client-id" },
            { "Spotify:CallbackUrl", "https://localhost:5001/auth/callback" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        _sut = new AuthController(
            _authClient,
            _cache,
            _configuration,
            NullLogger<AuthController>.Instance,
            TimeProvider.System);

        var session = Substitute.For<ISession>();
        session.Id.Returns(SessionId);
        session.LoadAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var httpContext = Substitute.For<HttpContext>();
        httpContext.Session.Returns(session);
        httpContext.Request.Headers.Returns(new HeaderDictionary());

        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    #region Logout Tests

    [Fact]
    public async Task Logout_WithClientKeyHeader_ClearsAuthCache()
    {
        // Arrange
        _sut.ControllerContext.HttpContext.Request.Headers["X-Client-Key"] = ClientKey;

        // Act
        var result = await _sut.Logout(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        await _cache.Received(1).RemoveAsync(
            $"{SpotifyAuthKeyPrefix}{ClientKey}",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Logout_WithSessionOnly_ClearsAuthCache()
    {
        // Arrange - No X-Client-Key header, will fall back to session

        // Act
        var result = await _sut.Logout(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        await _cache.Received(1).RemoveAsync(
            $"{SpotifyAuthKeyPrefix}{SessionId}",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Logout_ReturnsOkResult()
    {
        // Arrange
        _sut.ControllerContext.HttpContext.Request.Headers["X-Client-Key"] = ClientKey;

        // Act
        var result = await _sut.Logout(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task Logout_RemovesCorrectCacheEntry()
    {
        // Arrange
        var customClientKey = "custom-client-key";
        _sut.ControllerContext.HttpContext.Request.Headers["X-Client-Key"] = customClientKey;

        // Act
        await _sut.Logout(CancellationToken.None);

        // Assert
        await _cache.Received(1).RemoveAsync(
            $"{SpotifyAuthKeyPrefix}{customClientKey}",
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Claim Tests

    [Fact]
    public async Task Claim_WithMissingTransferToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new ClaimRequest(string.Empty);

        // Act
        var result = await _sut.Claim(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Claim_WithNullTransferToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new ClaimRequest(null);

        // Act
        var result = await _sut.Claim(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    #endregion
}
