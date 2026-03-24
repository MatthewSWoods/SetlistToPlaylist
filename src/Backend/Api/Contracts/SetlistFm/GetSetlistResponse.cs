using System.Text.Json.Serialization;

namespace SetlistToPlaylist.ApiService.Contracts.SetlistFm;

public class SetlistDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("versionId")]
    public string? VersionId { get; set; }
    [JsonPropertyName("eventDate")]
    public string? EventDate { get; set; }
    [JsonPropertyName("artist")]
    public ArtistDto? Artist { get; set; }
    [JsonPropertyName("venue")]
    public VenueDto? Venue { get; set; }
    [JsonPropertyName("tour")]
    public TourDto? Tour { get; set; }
    [JsonPropertyName("sets")]
    public SetsDto? Sets { get; set; }
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public sealed record GetSetlistResponse
{
    public SetlistDto? setlist { get; set; }
}
