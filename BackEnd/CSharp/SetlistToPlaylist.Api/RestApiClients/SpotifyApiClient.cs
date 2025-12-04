using System;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SetlistToPlaylist.Api.Models.SetlistFm;
using SetlistToPlaylist.Api.Models.Spotify;
using SetlistToPlaylist.Api.RestApiClients.Dto;
using SetlistToPlaylist.Api.RestApiClients.Interfaces;
using SetlistToPlaylist.Api.Settings;

namespace SetlistToPlaylist.Api.RestApiClients
{
    public class SpotifyApiClient : ISpotifyApiClient
    {
        private readonly ILogger<SpotifyApiClient> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<ApiSecrets> _apiSecrets;
        private readonly IOptions<ApiClientSettings> _apiSettings;

        public SpotifyApiClient(
            ILogger<SpotifyApiClient> logger,
            IHttpClientFactory httpClientFactory,
            IOptions<ApiSecrets> apiSecrets,
            IOptions<ApiClientSettings> apiSettings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiSecrets = apiSecrets;
            _apiSettings = apiSettings;
        }
        public async Task<string> GetCurrentUserIdAsync(string accessToken)
        {
            var client = _httpClientFactory.CreateClient("SpotifyApiClient");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetAsync("https://api.spotify.com/v1/me");
            response.EnsureSuccessStatusCode();

            var contentString = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<SpotifyUser>(contentString) ?? throw new SerializationException($"unable to parse response content: {contentString}");
            return user.Id ?? throw new NullReferenceException("user.Id is null");
        }

        public async Task<SpotifyPlaylist> CreateNewSpotifyPlaylistAsync(SpotifyAuth auth, string userId, string playlistName, string description)
        {
            var client = _httpClientFactory.CreateClient("SpotifyApiClient");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var playlistRequest = new SpotifyCreatePlaylistRequest()
            {
                Name = playlistName,
                Description = description,
                isPublic = false,
                isCollaborative = false
            };

            var requestUrl = $"{_apiSettings.Value.SpotifyBaseUrl}" + "users/" + $"{userId}/playlists";
            var requestContent = new StringContent(JsonConvert.SerializeObject(playlistRequest), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(requestUrl, requestContent);
            response.EnsureSuccessStatusCode();
            var contentString = await response.Content.ReadAsStringAsync();
 
            return JsonConvert.DeserializeObject<SpotifyPlaylist>(contentString) ?? throw new SerializationException($"unable to parse response content: {contentString}");
        }

        public async Task<(string[], string[])> FindSpotifyTracksFromSetlistAsync(SpotifyAuth auth, Setlist setlist)
        {
            var client = _httpClientFactory.CreateClient("SpotifyApiClient");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var songs = (setlist.Sets?.Set ?? throw new NullReferenceException("No Set found in list of sets"))
                .SelectMany(set => set.Song ?? throw new NullReferenceException("No Song found in setlist"))
                .Select(song => song.Name)
                .ToList();

            var spotifyTrackUris = new List<string>();
            var failedTracks = new List<string>();
            foreach (var song in songs)
            {
                var songNameEncoded = Uri.EscapeDataString(song ?? throw new NullReferenceException("No song found in setlist"));
                var artistNameEncoded = Uri.EscapeDataString(setlist.Artist?.Name ?? throw new NullReferenceException("No Artist Name found in setlist"));

                var requestUrl = $"{_apiSettings.Value.SpotifyBaseUrl}" +
                                 $"search?q=track:{songNameEncoded} artist:{artistNameEncoded}&type=track&limit=1";

                var response = await client.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var contentString = await response.Content.ReadAsStringAsync();
                var trackResponse = JsonConvert.DeserializeObject<SpotifySearchTracksResponse>(contentString) ?? throw new SerializationException($"unable to parse response content: {contentString}");

                if (trackResponse.Tracks!.Items!.Any())
                {
                    var foundTrack = trackResponse.Tracks!.Items!.First();
                    _logger.LogInformation($"Found track: {foundTrack.Name!} by {foundTrack.Artists!.First().Name}, " +
                        $"Appending song uri ({foundTrack.Uri}) to results");
                    spotifyTrackUris.Add(trackResponse.Tracks!.Items!.First().Uri!);
                }
                else
                {
                    _logger.LogError($"Unable to find track {song} from search");
                    failedTracks.Add(song);
                }
            }

            return (spotifyTrackUris.ToArray(), failedTracks.ToArray());
        }

        public async Task<HttpStatusCode> UpdatePlaylistTracksAsync(SpotifyAuth auth, string playlistId, string[] trackUris)
        {
            var client = _httpClientFactory.CreateClient("SpotifyApiClient");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var requestUrl = $"{_apiSettings.Value.SpotifyBaseUrl}" +
                $"playlists/" +
                $"{playlistId}" +
                $"/tracks";

            var payload = new { uris = trackUris };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var requestContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            _logger.LogInformation($"Update playlist tracks: {requestContent}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Content = requestContent;
            
            _logger.LogInformation("Sending Request: {request}", request);
            var response = await client.SendAsync(request);
            
            response.EnsureSuccessStatusCode();

            return response.StatusCode;
        }
    }
}
