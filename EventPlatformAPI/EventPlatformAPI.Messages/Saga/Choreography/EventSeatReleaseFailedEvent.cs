namespace EventPlatformAPI.Messages.Saga.Choreography;

public class EventSeatReleaseFailedEvent
{
    public Guid CorrelationId { get; set; }
    public Guid RegistrationId { get; set; }
    public int EventId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
