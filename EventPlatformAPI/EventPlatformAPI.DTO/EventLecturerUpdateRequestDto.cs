namespace EventPlatformAPI.DTO;

public class EventLecturerUpdateRequestDto
{
    public int EventId { get; set; }
    public int LecturerId { get; set; }
    public DateTime DateTime { get; set; }
    public string Theme { get; set; } = string.Empty;
}
