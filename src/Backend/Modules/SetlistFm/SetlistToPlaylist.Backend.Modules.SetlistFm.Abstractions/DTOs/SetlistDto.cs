using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

public class SetlistDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the setlist.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    /// <summary>
    /// Gets or sets the unique identifier for the version of the setlist.
    /// </summary>
    [JsonPropertyName("versionId")]
    public string? VersionId { get; set; }
    /// <summary>
    /// Gets or sets the date of the gig, represented as a string.
    /// </summary>
    [JsonPropertyName("eventDate")]
    public string? EventDate { get; set; }
    /// <summary>
    /// Gets or sets the artist associated with the gig.
    /// </summary>
    [JsonPropertyName("artist")]
    public ArtistDto? Artist { get; set; }
    /// <summary>
    /// Gets or sets the venue associated with the event.
    /// </summary>
    [JsonPropertyName("venue")]
    public VenueDto? Venue { get; set; }
    /// <summary>
    /// Gets or sets the tour information associated with this event.
    /// </summary>
    [JsonPropertyName("tour")]
    public TourDto? Tour { get; set; }
    /// <summary>
    /// Gets or sets the collection of sets associated with this gig.
    /// </summary>
    [JsonPropertyName("sets")]
    public SetsDto? Sets { get; set; }
    /// <summary>
    /// Gets or sets the URL associated with the setlist.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
