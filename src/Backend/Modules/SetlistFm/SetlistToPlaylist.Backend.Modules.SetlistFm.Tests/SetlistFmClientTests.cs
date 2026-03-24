using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;
using SetlistToPlaylist.Backend.Modules.SetlistFm.Clients;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Tests;

public sealed class SetlistFmClientTests
{
    private const string BaseUrl = "https://api.setlist.fm/rest/1.0/";
    private const string SetlistId = "63eb7e6b";

    private static readonly SetlistDto SampleSetlist = new()
    {
        Id = SetlistId,
        EventDate = "15-06-2016",
        Artist = new ArtistDto { Name = "Radiohead" },
        Venue = new VenueDto { Name = "Roundhouse", City = new CityDto { Name = "London" } },
        Sets = new SetsDto
        {
            Set = [new SetDto { Song = [new SongDto { Name = "Creep" }, new SongDto { Name = "Karma Police" }] }]
        }
    };

    private static SetlistFmClient BuildClient(MockHttpMessageHandler handler)
    {
        var httpClient = handler.ToHttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);
        return new SetlistFmClient(httpClient, NullLogger<SetlistFmClient>.Instance);
    }

    [Fact]
    public async Task GetSetlistByIdAsync_SuccessResponse_ReturnsSetlist()
    {
        var json = JsonSerializer.Serialize(SampleSetlist);
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}setlist/{SetlistId}")
            .Respond(HttpStatusCode.OK, "application/json", json);

        var result = await BuildClient(handler).GetSetlistByIdAsync(SetlistId);

        Assert.True(result.IsSuccess);
        Assert.Equal(SetlistId, result.Value.Id);
        Assert.Equal("Radiohead", result.Value.Artist?.Name);
    }

    [Fact]
    public async Task GetSetlistByIdAsync_NotFoundResponse_ReturnsFailWithMessage()
    {
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}setlist/{SetlistId}")
            .Respond(HttpStatusCode.NotFound);

        var result = await BuildClient(handler).GetSetlistByIdAsync(SetlistId);

        Assert.True(result.IsFailed);
        Assert.Contains("not found", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
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

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task GetSetlistByIdAsync_NetworkException_ReturnsFail()
    {
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}setlist/{SetlistId}")
            .Throw(new HttpRequestException("Connection refused"));

        var result = await BuildClient(handler).GetSetlistByIdAsync(SetlistId);

        Assert.True(result.IsFailed);
        Assert.Contains("Failed to contact", result.Errors[0].Message);
    }

    [Fact]
    public async Task GetSetlistByIdAsync_EmptyBody_ReturnsFail()
    {
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}setlist/{SetlistId}")
            .Respond(HttpStatusCode.OK, "application/json", "null");

        var result = await BuildClient(handler).GetSetlistByIdAsync(SetlistId);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task GetSetlistByIdAsync_SongsAreDeserializedCorrectly()
    {
        var json = JsonSerializer.Serialize(SampleSetlist);
        using var handler = new MockHttpMessageHandler();
        handler.When($"{BaseUrl}setlist/{SetlistId}")
            .Respond(HttpStatusCode.OK, "application/json", json);

        var result = await BuildClient(handler).GetSetlistByIdAsync(SetlistId);

        Assert.True(result.IsSuccess);
        var songs = result.Value.Sets?.Set?.SelectMany(s => s.Song ?? []).ToArray();
        Assert.Equal(2, songs?.Length);
        Assert.Equal("Creep", songs![0].Name);
        Assert.Equal("Karma Police", songs[1].Name);
    }
}
