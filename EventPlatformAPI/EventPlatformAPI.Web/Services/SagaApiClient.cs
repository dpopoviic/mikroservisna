using EventPlatformAPI.DTO;

namespace EventPlatformAPI.Web.Services
{
    public class SagaApiClient : ISagaApiClient
    {
        private readonly HttpClient _httpClient;

        public SagaApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PublishEventResultDto?> PublishEventAsync(PublishEventRequestDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/events/publish", request);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<PublishEventResultDto>();
        }
    }
}
