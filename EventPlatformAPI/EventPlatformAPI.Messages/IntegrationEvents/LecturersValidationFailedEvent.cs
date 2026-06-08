namespace EventPlatformAPI.Messages.IntegrationEvents;

public class LecturersValidationFailedEvent
{
    public Guid CorrelationId { get; set; }
    public List<int> InvalidLecturerIds { get; set; } = [];
    public string Reason { get; set; } = string.Empty;
}