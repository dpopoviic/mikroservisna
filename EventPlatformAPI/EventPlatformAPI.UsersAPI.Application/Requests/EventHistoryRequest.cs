namespace EventPlatformAPI.UsersAPI.Application.Requests
{
    public record EventHistoryRequest(
       string EventType,
       int Version,
       Guid CorrelationId,
       DateTime OccurredOn);

}
