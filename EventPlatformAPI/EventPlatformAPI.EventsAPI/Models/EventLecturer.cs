namespace EventPlatformAPI.EventsAPI.Models;

public class EventLecturer
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int LecturerId { get; set; }
    public DateTime DateTime { get; set; }
    public string Theme { get; set; } = string.Empty;

    public Event? Event { get; set; }
}
