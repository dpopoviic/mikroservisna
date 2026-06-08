namespace EventPlatformAPI.EventsAPI.Models;

public class RegistrationSagaState
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid RegistrationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
