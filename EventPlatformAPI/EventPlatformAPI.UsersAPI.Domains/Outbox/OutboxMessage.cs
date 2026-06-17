namespace EventPlatformAPI.UsersAPI.Domains.Outbox;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public bool IsPublished { get; set; }
}
