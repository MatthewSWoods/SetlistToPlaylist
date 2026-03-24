using System.Text.Json.Serialization;

namespace SetlistToPlaylist.ApiService.Contracts.SetlistFm;

public class SetsDto
{
    [JsonPropertyName("set")]
    public SetDto[]? Set { get; set; }
}
