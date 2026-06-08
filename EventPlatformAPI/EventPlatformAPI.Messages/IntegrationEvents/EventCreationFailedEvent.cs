namespace EventPlatformAPI.Messages.IntegrationEvents;

public class EventCreationFailedEvent
{
    public Guid CorrelationId { get; set; }
    public string Reason { get; set; } = string.Empty;
}