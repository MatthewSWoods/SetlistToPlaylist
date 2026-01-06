using System.Text.Json.Serialization;

namespace SetlistToPlaylist.Backend.Modules.Spotify.Abstractions.DTOs;

public sealed record AuthDto
{
    /// <summary>
    /// Gets or sets the OAuth 2.0 access token used to authenticate API requests.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
    /// <summary>
    /// Gets or sets the type of the authentication token returned by the service.
    /// </summary>
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
    /// <summary>
    /// Gets or sets the number of seconds until the token expires.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; } = 0;
    [JsonPropertyName("refresh_token")]
    /// <summary>
    /// Gets or sets the OAuth 2.0 refresh token used to obtain new access tokens.
    /// </summary>
    public string? RefreshToken { get; set; }
    /// <summary>
    /// Gets or sets the expiration time for the item.
    /// </summary>
    /// <remarks>If <see langword="null"/> is assigned, the item does not expire. The default value is <see
    /// cref="DateTime.UnixEpoch"/>, which may indicate no expiration depending on usage context.</remarks>
    public DateTime? ExpiryTime { get; set; } = DateTime.UnixEpoch;
}