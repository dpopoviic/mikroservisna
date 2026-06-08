namespace EventPlatformAPI.Messages.IntegrationEvents;

public class EventCreatedEvent
{
    public Guid CorrelationId { get; set; }
    public int EventId { get; set; }
}