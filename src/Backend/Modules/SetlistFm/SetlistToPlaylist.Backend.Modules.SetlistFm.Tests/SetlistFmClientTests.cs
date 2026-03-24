using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Clients;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Tests.Builders;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Tests;

public sealed class SetlistFmClientTests
{
    private const string BaseUrl = "https://api.setlist.fm/rest/1.0/";
    private const string SetlistId = "63eb7e6b";

    private static SetlistFmClient BuildClient(MockHttpMessageHandler handler)
    {
        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);
        return new SetlistFmClient(httpClient, NullLogger<SetlistFmClient>.Instance);
    }

    [Fact]
    public async Task GetSetlistByIdAsync_SuccessResponse_ReturnsSetlist()
    {
        var setlist = new SetlistDtoBuilder()
            .WithId(SetlistId)
            .WithArtist("Radiohead")
            .WithSongNames("Creep", "Karma Police")
            .Build();
        var json = JsonSerializer.Serialize(setlist);

        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}setlist/{SetlistId}")
            .Respond(HttpStatusCode.OK, "application/json", json);

        var result = await BuildClient(handler).GetSetlistByIdAsync(SetlistId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(SetlistId);
        result.Value.Artist?.Name.ShouldBe("Radiohead");
    }

    [Fact]
    public async Task GetSetlistByIdAsync_NotFoundResponse_ReturnsFailWithMessage()
    {
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}setlist/{SetlistId}")
            .Respond(HttpStatusCode.NotFound);

        var result = await BuildClient(handler).GetSetlistByIdAsync(SetlistId);

        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("not found");
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task GetSetlistByIdAsync_UpstreamError_ReturnsFail(HttpStatusCode statusCode)
    {
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}setlist/{SetlistId}")
            .Respond(statusCode);

        var result = await BuildClient(handler).GetSetlistByIdAsync(SetlistId);

        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task GetSetlistByIdAsync_NetworkException_ReturnsFail()
    {
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}setlist/{SetlistId}")
            .Throw(new HttpRequestException("Connection refused"));

        var result = await BuildClient(handler).GetSetlistByIdAsync(SetlistId);

        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("Failed to contact");
    }

    [Fact]
    public async Task GetSetlistByIdAsync_EmptyBody_ReturnsFail()
    {
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}setlist/{SetlistId}")
            .Respond(HttpStatusCode.OK, "application/json", "null");

        var result = await BuildClient(handler).GetSetlistByIdAsync(SetlistId);

        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task GetSetlistByIdAsync_SongsAreDeserializedCorrectly()
    {
        var setlist = new SetlistDtoBuilder()
            .WithId(SetlistId)
            .WithSongNames("Creep", "Karma Police")
            .Build();
        var json = JsonSerializer.Serialize(setlist);

        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}setlist/{SetlistId}")
            .Respond(HttpStatusCode.OK, "application/json", json);

        var result = await BuildClient(handler).GetSetlistByIdAsync(SetlistId);

        result.IsSuccess.ShouldBeTrue();
        var songs = result.Value.Sets?.Set?.SelectMany(s => s.Song ?? []).ToArray();
        songs.ShouldNotBeNull();
        songs.Length.ShouldBe(2);
        songs[0].Name.ShouldBe("Creep");
        songs[1].Name.ShouldBe("Karma Police");
    }
}
