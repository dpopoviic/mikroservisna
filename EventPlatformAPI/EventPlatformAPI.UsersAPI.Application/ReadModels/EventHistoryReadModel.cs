namespace EventPlatformAPI.UsersAPI.Application.ReadModels
{
    public record EventHistoryReadModel(
       string EventType,
       int Version,
       Guid CorrelationId,
       DateTime OccurredOn);

}
