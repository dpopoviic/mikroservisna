namespace EventPlatformAPI.DTO;

public class EventCreateRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Agenda { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public decimal DurationInHours { get; set; }
    public decimal Price { get; set; }
    public int TypeId { get; set; }
    public int LocationId { get; set; }
    public ICollection<int> LecturerIds { get; set; } = new List<int>();
}
