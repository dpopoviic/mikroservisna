using System.ComponentModel.DataAnnotations;

namespace EventPlatformAPI.Web.Domains;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "Trajanje mora biti veće od 0.")]
    public decimal DurationInHours { get; set; }
    [Range(typeof(decimal), "0", "9999999", ErrorMessage = "Cena ne može biti negativna.")]
    public decimal Price { get; set; }
    public string Agenda { get; set; } = string.Empty;

    public int TypeId { get; set; }
    public int LocationId { get; set; }

    public EventType? Type { get; set; }
    public Location? Location { get; set; }
    public ICollection<EventLecturer> EventLecturers { get; set; } = new List<EventLecturer>();
}
