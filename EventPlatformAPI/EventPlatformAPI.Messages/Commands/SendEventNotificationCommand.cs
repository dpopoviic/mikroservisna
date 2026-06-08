namespace EventPlatformAPI.Messages.Commands;

public class SendEventNotificationCommand
{
    public Guid CorrelationId { get; set; }
    public int EventId { get; set; }
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}