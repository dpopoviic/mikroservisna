namespace EventPlatformAPI.Messages.IntegrationEvents;

public class LecturersValidatedEvent
{
    public Guid CorrelationId { get; set; }
    public List<int> LecturerIds { get; set; } = [];
}