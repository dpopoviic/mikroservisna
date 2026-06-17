namespace EventPlatformAPI.UsersAPI.Domains.Outbox;

public class ChoreographyProcessState
{
    public long Id { get; set; }
    public Guid CorrelationId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
