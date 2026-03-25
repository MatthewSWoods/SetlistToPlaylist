using Microsoft.Extensions.Time.Testing;
using SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.Spotify.Extensions;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Tests;

public sealed class AuthExtensionsTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SetExpiryTime_SetsExpiryToNowPlusExpiresInMinusBuffer()
    {
        var timeProvider = new FakeTimeProvider(FixedNow);
        var auth = new AuthDto { ExpiresIn = 3600 };

        auth.SetExpiryTime(timeProvider);

        // Buffer is 60s: expected = FixedNow + 3600s - 60s = FixedNow + 3540s
        auth.ExpiryTime.ShouldBe(FixedNow.UtcDateTime.AddSeconds(3540));
    }

    [Fact]
    public void SetExpiryTime_NullExpiresIn_TokenIsImmediatelyExpired()
    {
        var timeProvider = new FakeTimeProvider(FixedNow);
        var auth = new AuthDto { ExpiresIn = null };

        auth.SetExpiryTime(timeProvider);

        // ExpiresIn ?? 0 minus 60s buffer = -60s from now, already expired
        auth.IsTokenExpired(timeProvider).ShouldBeTrue();
    }

    [Fact]
    public void IsTokenExpired_ExpiryInThePast_ReturnsTrue()
    {
        var timeProvider = new FakeTimeProvider(FixedNow);
        var auth = new AuthDto { ExpiryTime = FixedNow.UtcDateTime.AddSeconds(-1) };

        auth.IsTokenExpired(timeProvider).ShouldBeTrue();
    }

    [Fact]
    public void IsTokenExpired_ExpiryInTheFuture_ReturnsFalse()
    {
        var timeProvider = new FakeTimeProvider(FixedNow);
        var auth = new AuthDto { ExpiryTime = FixedNow.UtcDateTime.AddHours(1) };

        auth.IsTokenExpired(timeProvider).ShouldBeFalse();
    }

    [Fact]
    public void IsTokenExpired_NullExpiryTime_ReturnsTrue()
    {
        var timeProvider = new FakeTimeProvider(FixedNow);
        var auth = new AuthDto { ExpiryTime = null };

        auth.IsTokenExpired(timeProvider).ShouldBeTrue();
    }

    [Fact]
    public void IsTokenExpired_ExactlyNow_ReturnsTrue()
    {
        var timeProvider = new FakeTimeProvider(FixedNow);
        var auth = new AuthDto { ExpiryTime = FixedNow.UtcDateTime };

        // >= means exactly now is also expired
        auth.IsTokenExpired(timeProvider).ShouldBeTrue();
    }
}
