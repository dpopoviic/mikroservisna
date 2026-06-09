namespace EventPlatformAPI.EventsAPI.Models;

public enum RegistrationStatus
{
    Pending,
    Validated,
    SeatReserved,
    Completed,
    Cancelled,
    Failed
}

public class Registration
{
    public Guid Id { get; set; }
    public int EventId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public RegistrationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public Event? Event { get; set; }
}
