namespace EventPlatformAPI.Web.Domains;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Capacity { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
