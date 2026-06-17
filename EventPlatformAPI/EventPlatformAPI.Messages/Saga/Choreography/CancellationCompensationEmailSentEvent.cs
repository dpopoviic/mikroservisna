namespace EventPlatformAPI.Messages.Saga.Choreography;

public class CancellationCompensationEmailSentEvent
{
    public Guid CorrelationId { get; set; }
    public Guid RegistrationId { get; set; }
    public Guid UserId { get; set; }
    public int EventId { get; set; }
    public DateTime Timestamp { get; set; }
}
