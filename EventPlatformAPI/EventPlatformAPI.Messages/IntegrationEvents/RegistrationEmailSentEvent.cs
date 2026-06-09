namespace EventPlatformAPI.Messages.IntegrationEvents;

public class RegistrationEmailSentEvent
{
    public Guid CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid RegistrationId { get; set; }
    public int EventId { get; set; }
}
