using SetlistToPlaylist.Backend.Modules.Spotify.Extensions;
using SetlistToPlaylist.Backend.Modules.Spotify.Tests.Builders;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Tests;

public sealed class AuthExtensionsTests
{
    [Fact]
    public void SetExpiryTime_SetsExpiryToUtcNowPlusExpiresInMinusBuffer()
    {
        var auth = new AuthDtoBuilder().Build();
        auth.ExpiresIn = 3600;
        var before = DateTime.UtcNow;

        auth.SetExpiryTime();

        var after = DateTime.UtcNow;
        // Buffer is 60s, so expected window is [before + 3540s, after + 3540s]
        auth.ExpiryTime.ShouldNotBeNull();
        auth.ExpiryTime!.Value.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(3540));
        auth.ExpiryTime!.Value.ShouldBeLessThanOrEqualTo(after.AddSeconds(3540));
    }

    [Fact]
    public void SetExpiryTime_NullExpiresIn_TokenIsImmediatelyExpired()
    {
        var auth = new AuthDtoBuilder().Build();
        auth.ExpiresIn = null;

        auth.SetExpiryTime();

        // ExpiresIn ?? 0 means 0 - 60s = already expired
        auth.IsTokenExpired().ShouldBeTrue();
    }

    [Fact]
    public void IsTokenExpired_PastExpiryTime_ReturnsTrue()
    {
        var auth = new AuthDtoBuilder().AsExpired().Build();

        auth.IsTokenExpired().ShouldBeTrue();
    }

    [Fact]
    public void IsTokenExpired_FutureExpiryTime_ReturnsFalse()
    {
        var auth = new AuthDtoBuilder().Build();

        auth.IsTokenExpired().ShouldBeFalse();
    }

    [Fact]
    public void IsTokenExpired_NullExpiryTime_ReturnsTrue()
    {
        var auth = new AuthDtoBuilder().Build();
        auth.ExpiryTime = null;

        auth.IsTokenExpired().ShouldBeTrue();
    }

    [Fact]
    public void IsTokenExpired_ExactlyNow_ReturnsTrue()
    {
        var auth = new AuthDtoBuilder().Build();
        auth.ExpiryTime = DateTime.UtcNow.AddMilliseconds(-1);

        auth.IsTokenExpired().ShouldBeTrue();
    }
}
