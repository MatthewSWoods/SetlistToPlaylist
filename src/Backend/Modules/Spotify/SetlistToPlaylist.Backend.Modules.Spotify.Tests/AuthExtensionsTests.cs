using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.Spotify.Extensions;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Tests;

public sealed class AuthExtensionsTests
{
    [Fact]
    public void SetExpiryTime_SetsExpiryToUtcNowPlusExpiresInMinusBuffer()
    {
        var auth = new AuthDto { ExpiresIn = 3600 };
        var before = DateTime.UtcNow;

        auth.SetExpiryTime();

        var after = DateTime.UtcNow;
        // Buffer is 60s, so expected window is [before + 3540s, after + 3540s]
        Assert.NotNull(auth.ExpiryTime);
        Assert.True(auth.ExpiryTime >= before.AddSeconds(3540));
        Assert.True(auth.ExpiryTime <= after.AddSeconds(3540));
    }

    [Fact]
    public void SetExpiryTime_NullExpiresIn_SetsExpiryToUnixEpochArea()
    {
        var auth = new AuthDto { ExpiresIn = null };
        auth.SetExpiryTime();

        // ExpiresIn ?? 0 means 0 - 60 = -60s from now, so it will be expired immediately
        Assert.True(auth.IsTokenExpired());
    }

    [Fact]
    public void IsTokenExpired_PastExpiryTime_ReturnsTrue()
    {
        var auth = new AuthDto { ExpiryTime = DateTime.UtcNow.AddSeconds(-1) };
        Assert.True(auth.IsTokenExpired());
    }

    [Fact]
    public void IsTokenExpired_FutureExpiryTime_ReturnsFalse()
    {
        var auth = new AuthDto { ExpiryTime = DateTime.UtcNow.AddHours(1) };
        Assert.False(auth.IsTokenExpired());
    }

    [Fact]
    public void IsTokenExpired_NullExpiryTime_ReturnsTrue()
    {
        var auth = new AuthDto { ExpiryTime = null };
        Assert.True(auth.IsTokenExpired());
    }

    [Fact]
    public void IsTokenExpired_ExactlyNow_ReturnsTrue()
    {
        var auth = new AuthDto { ExpiryTime = DateTime.UtcNow.AddMilliseconds(-1) };
        Assert.True(auth.IsTokenExpired());
    }
}
