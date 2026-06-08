namespace EventPlatformAPI.Messages.IntegrationEvents;

public class LocationValidationFailedEvent
{
    public Guid CorrelationId { get; set; }
    public int LocationId { get; set; }
    public string Reason { get; set; } = string.Empty;
}