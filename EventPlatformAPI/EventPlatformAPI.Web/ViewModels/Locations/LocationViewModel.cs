using System.ComponentModel.DataAnnotations;

namespace EventPlatformAPI.Web.ViewModels.Locations;

public class LocationViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    [Range(1, int.MaxValue, ErrorMessage = "Kapacitet mora biti ve?i od 0.")]
    public int Capacity { get; set; }
}
