namespace EventPlatformAPI.Messages.IntegrationEvents;

public class LocationValidatedEvent
{
    public Guid CorrelationId { get; set; }
    public int LocationId { get; set; }
}