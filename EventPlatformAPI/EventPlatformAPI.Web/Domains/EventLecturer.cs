namespace EventPlatformAPI.Web.Domains;

public class EventLecturer
{
    public int EventId { get; set; }
    public int LecturerId { get; set; }
    public DateTime DateTime { get; set; }
    public string Theme { get; set; } = string.Empty;

    public Event? Event { get; set; }
    public Lecturer? Lecturer { get; set; }
}
