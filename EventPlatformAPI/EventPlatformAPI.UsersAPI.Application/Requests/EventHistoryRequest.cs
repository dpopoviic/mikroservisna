namespace EventPlatformAPI.UsersAPI.Application.ReadModels
{
    public record EventHistoryRequest(
       string EventType,
       int Version,
       Guid CorrelationId,
       DateTime OccurredOn);

}
