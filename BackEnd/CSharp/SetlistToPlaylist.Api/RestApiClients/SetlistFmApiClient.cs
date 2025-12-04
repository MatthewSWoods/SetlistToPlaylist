using System.Runtime.Serialization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SetlistToPlaylist.Api.Models.SetlistFm;
using SetlistToPlaylist.Api.RestApiClients.Interfaces;
using SetlistToPlaylist.Api.Settings;

namespace SetlistToPlaylist.Api.RestApiClients
{
    public class SetlistFmApiClient : ISetlistFmApiClient
    {
        private readonly ILogger<SetlistFmApiClient> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<ApiClientSettings> _apiClientSettings;
        private readonly IOptions<ApiSecrets> _apiSecrets;

        public SetlistFmApiClient(
            ILogger<SetlistFmApiClient> logger,
            IHttpClientFactory httpClientFactory,
            IOptions<ApiClientSettings> apiClientSettings,
            IOptions<ApiSecrets> apiSecrets)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiClientSettings = apiClientSettings;
            _apiSecrets = apiSecrets;
        }

        public async Task<Setlist> GetSetlistFromUrlAsync(string url)
        {
            var client = _httpClientFactory.CreateClient("SetlistFmApiClient");

            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("x-api-key", _apiSecrets.Value.SetlistFmApiKey);

            var setlistId = url.Split("-").Last().Replace(".html", string.Empty);
            var requestUrl = new Uri(_apiClientSettings.Value.SetlistFmBaseUrl + "/setlist/" + setlistId);

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            _logger.LogInformation("Sending Request: {request}", request);
            var response  = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var setlistJson = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Setlist>(setlistJson) ?? throw new SerializationException($"Unable to deserialize the response content {setlistJson}");
        }
    }
}
