namespace EventPlatformAPI.Web.Domains;

public class Lecturer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;

    public ICollection<EventLecturer> EventLecturers { get; set; } = new List<EventLecturer>();
}
