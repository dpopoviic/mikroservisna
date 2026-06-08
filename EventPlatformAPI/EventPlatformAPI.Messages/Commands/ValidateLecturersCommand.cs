namespace EventPlatformAPI.Messages.Commands;

public class ValidateLecturersCommand
{
    public Guid CorrelationId { get; set; }
    public List<int> LecturerIds { get; set; } = [];
}