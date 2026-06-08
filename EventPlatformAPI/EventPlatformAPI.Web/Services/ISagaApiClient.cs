using EventPlatformAPI.DTO;

namespace EventPlatformAPI.Web.Services
{
    public interface ISagaApiClient
    {
        Task<PublishEventResultDto?> PublishEventAsync(PublishEventRequestDto request);

    }
}
