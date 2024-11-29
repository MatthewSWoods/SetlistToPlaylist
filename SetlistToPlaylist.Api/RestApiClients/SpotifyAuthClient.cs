using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SetlistToPlaylist.Api.Models.Spotify;
using SetlistToPlaylist.Api.RestApiClients.Interfaces;
using SetlistToPlaylist.Api.Settings;

namespace SetlistToPlaylist.Api.RestApiClients
{
    public class SpotifyAuthClient : ISpotifyAuthClient
    {
        private readonly ILogger<SpotifyAuthClient> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<ApiSecrets> _apiSecrets;
        private readonly IOptions<ApiClientSettings> _apiSettings;

        public SpotifyAuthClient(
            ILogger<SpotifyAuthClient> logger,
            IHttpClientFactory httpClientFactory,
            IOptions<ApiSecrets> apiSecrets,
            IOptions<ApiClientSettings> apiSettings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiSecrets = apiSecrets;
            _apiSettings = apiSettings;
        }

        public async Task<SpotifyAuth> GetTokenAsync(string code)
        {
            var clientId = _apiSecrets.Value.SpotifyClientId ?? throw new NullReferenceException("SpotifyClientId cannot be null");
            var clientSecret = _apiSecrets.Value.SpotifyClientSecret ?? throw new NullReferenceException("SpotifyClientSecret cannot be null");
            var redirectUri = _apiSettings.Value.SpotifyRedirectUri ?? throw new NullReferenceException("SpotifyRedirectUri cannot be null");

            var client = _httpClientFactory.CreateClient("SpotifyAuthClient");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            var authorizationHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authorizationHeader);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri }
            });

            _logger.LogInformation("Sending Request: {request}", request);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"status code: {response.StatusCode}, content: {responseString}");
            return JsonConvert.DeserializeObject<SpotifyAuth>(responseString) ?? throw new InvalidOperationException($"Unable to deserialize responseString {responseString} to type SpotifyAuth");
        }

        public async Task<SpotifyAuth> RefreshTokenAsync(SpotifyAuth spotifyAuth)
        {
            var refreshToken = spotifyAuth.RefreshToken ?? throw new NullReferenceException("Refresh Token cannot be null");
            var clientId = _apiSecrets.Value.SpotifyClientId;
            var clientSecret = _apiSecrets.Value.SpotifyClientSecret;

            var client = _httpClientFactory.CreateClient("SpotifyAuthClient");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            var authorizationHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authorizationHeader);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            });
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

            _logger.LogInformation("Sending Request: {request}", request);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var contentString = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"status code: {response.StatusCode}");
            return JsonConvert.DeserializeObject<SpotifyAuth>(contentString) ?? throw new SerializationException($"Unable to deserialize contentString {contentString} to type SpotifyAuth");
        }

        public string CreateOAuthRequestUrl()
        {
            var encodedRedirect = Uri.EscapeDataString(_apiSettings.Value.SpotifyRedirectUri);
            var encodedScope = Uri.EscapeDataString("user-read-private user-read-email playlist-modify-private playlist-modify-public playlist-read-private playlist-read-collaborative user-library-modify user-library-read");

            var requestUrl = $"{_apiSettings.Value.SpotifyAuthUri}" +
                             $"?client_id={_apiSecrets.Value.SpotifyClientId}" +
                             $"&response_type=code" +
                             $"&redirect_uri={encodedRedirect}" +
                             $"&show_dialog=true" +
                             $"&scope={encodedScope}";

            _logger.LogInformation($"Targeting Url for OAuth: {requestUrl}");


            return requestUrl;
        }

        public (string, string) AddStateToOAuthRequestUrl(string url)
        {
            var encodedState = GenerateRandomString(16);
            var requestUrl = url + $"&state={encodedState}";
            
            return (requestUrl, encodedState);
        }
        
        // Helper method to generate a random string for state
        private static string GenerateRandomString(int length)
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[length];
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
