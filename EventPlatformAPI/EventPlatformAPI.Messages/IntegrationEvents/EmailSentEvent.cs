namespace EventPlatformAPI.Messages.IntegrationEvents;

public class EmailSentEvent
{
    public Guid CorrelationId { get; set; }
    public int EventId { get; set; }
}