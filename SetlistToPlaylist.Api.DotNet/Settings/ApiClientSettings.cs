namespace SetlistToPlaylist.Api.Settings
{
    public class ApiClientSettings
    {
        public required string SpotifyBaseUrl { get; set; }
        public required string SpotifyAuthUri { get; set; }
        public required string SpotifyRedirectUri { get; set; }
        public required string SetlistFmBaseUrl { get; set; }
    }
}
