using System.ComponentModel.DataAnnotations;

namespace EventPlatformAPI.Web.Domains;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    [Range(1, int.MaxValue, ErrorMessage = "Kapacitet mora biti veći od 0.")]
    public int Capacity { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
