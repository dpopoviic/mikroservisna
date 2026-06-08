namespace EventPlatformAPI.SagaOrcgestrator.Models;

public class PublishEventSaga
{
    public long Id { get; set; }
    public Guid CorrelationId { get; set; }
    public int? EventId { get; set; }
    public string Status { get; set; } = PublishEventSagaStatus.Started;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
}

public static class PublishEventSagaStatus
{
    public const string Started = "Started";
    public const string LocationValidated = "LocationValidated";
    public const string LecturersValidated = "LecturersValidated";
    public const string EventCreated = "EventCreated";
    public const string EmailSent = "EmailSent";
    public const string Completed = "Completed";
    public const string Compensating = "Compensating";
    public const string Compensated = "Compensated";
    public const string Failed = "Failed";
}