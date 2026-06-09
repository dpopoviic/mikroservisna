namespace EventPlatformAPI.EventsAPI.Models;

public class SagaEventLog
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
