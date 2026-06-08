namespace EventPlatformAPI.Messages.Commands;

public class CancelEventCommand
{
    public Guid CorrelationId { get; set; }
    public int EventId { get; set; }
    public string Reason { get; set; } = string.Empty;
}