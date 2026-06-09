namespace EventPlatformAPI.Messages.IntegrationEvents;

public class RegistrationEmailFailedEvent
{
    public Guid CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid RegistrationId { get; set; }
    public int EventId { get; set; }
    public string FailureReason { get; set; } = string.Empty;
}
