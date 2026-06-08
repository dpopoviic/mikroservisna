namespace EventPlatformAPI.Messages.Commands;

public class ValidateLocationCommand
{
    public Guid CorrelationId { get; set; }
    public int LocationId { get; set; }
}