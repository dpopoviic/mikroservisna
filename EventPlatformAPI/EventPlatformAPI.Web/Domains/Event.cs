namespace EventPlatformAPI.Web.Domains;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public decimal DurationInHours { get; set; }
    public decimal Price { get; set; }
    public string Agenda { get; set; } = string.Empty;

    public int TypeId { get; set; }
    public int LocationId { get; set; }

    public EventType? Type { get; set; }
    public Location? Location { get; set; }
    public ICollection<EventLecturer> EventLecturers { get; set; } = new List<EventLecturer>();
}
