namespace EventPlatformAPI.Messages.IntegrationEvents;

public class EventCancelledEvent
{
    public Guid CorrelationId { get; set; }
    public int EventId { get; set; }
}