using FluentResults;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.Clients;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Services;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Tests.Builders;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Tests;

public sealed class SetlistFmServiceTests
{
    private readonly ISetlistFmClient _client = Substitute.For<ISetlistFmClient>();
    private readonly SetlistFmService _sut;

    public SetlistFmServiceTests()
    {
        _sut = new SetlistFmService(_client, NullLogger<SetlistFmService>.Instance);
    }

    [Theory]
    [InlineData(
        "https://www.setlist.fm/setlist/radiohead/2016/roundhouse-london-england-63eb7e6b.html",
        "63eb7e6b")]
    [InlineData(
        "https://www.setlist.fm/setlist/foo-fighters/2023/some-venue-city-country-1a2b3c4d.html",
        "1a2b3c4d")]
    [InlineData(
        "https://www.setlist.fm/setlist/the-cure/2019/alexandra-palace-london-england-abcdef01.html",
        "abcdef01")]
    public async Task GetSetlistAsync_ValidUrl_ExtractsCorrectIdAndCallsClient(string url, string expectedId)
    {
        var setlist = new SetlistDtoBuilder().WithId(expectedId).Build();
        _client.GetSetlistByIdAsync(expectedId, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(setlist));

        var result = await _sut.GetSetlistAsync(url);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(expectedId);
        await _client.Received(1).GetSetlistByIdAsync(expectedId, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("https://open.spotify.com/playlist/123")]
    [InlineData("https://setlist.fm/setlist/artist-63eb7e6b.html")]    // missing www
    [InlineData("https://api.setlist.fm/rest/1.0/setlist/63eb7e6b")]   // API URL, not webpage
    public async Task GetSetlistAsync_NonSetlistFmHost_ReturnsFail(string url)
    {
        var result = await _sut.GetSetlistAsync(url);

        result.IsFailed.ShouldBeTrue();
        await _client.DidNotReceiveWithAnyArgs().GetSetlistByIdAsync(default!, default);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSetlistAsync_MalformedUrl_ReturnsFail(string url)
    {
        var result = await _sut.GetSetlistAsync(url);

        result.IsFailed.ShouldBeTrue();
        await _client.DidNotReceiveWithAnyArgs().GetSetlistByIdAsync(default!, default);
    }

    [Fact]
    public async Task GetSetlistAsync_UrlWithoutIdPattern_ReturnsFail()
    {
        var result = await _sut.GetSetlistAsync("https://www.setlist.fm/artist/radiohead.html");

        result.IsFailed.ShouldBeTrue();
        await _client.DidNotReceiveWithAnyArgs().GetSetlistByIdAsync(default!, default);
    }

    [Fact]
    public async Task GetSetlistAsync_ClientFails_PropagatesFailure()
    {
        _client.GetSetlistByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("Setlist '63eb7e6b' not found on Setlist.fm"));

        var result = await _sut.GetSetlistAsync(
            "https://www.setlist.fm/setlist/radiohead/2016/roundhouse-63eb7e6b.html");

        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("not found");
    }
}
