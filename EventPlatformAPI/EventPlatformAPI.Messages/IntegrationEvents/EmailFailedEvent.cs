namespace EventPlatformAPI.Messages.IntegrationEvents;

public class EmailFailedEvent
{
    public Guid CorrelationId { get; set; }
    public int EventId { get; set; }
    public string Reason { get; set; } = string.Empty;
}