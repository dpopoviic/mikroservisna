using System.ComponentModel.DataAnnotations;
using EventPlatformAPI.Web.ViewModels.EventLecturers;

namespace EventPlatformAPI.Web.ViewModels.Events;

public class EventViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "Trajanje mora biti ve?e od 0.")]
    public decimal DurationInHours { get; set; }
    [Range(typeof(decimal), "0", "9999999", ErrorMessage = "Cena ne može biti negativna.")]
    public decimal Price { get; set; }
    public string Agenda { get; set; } = string.Empty;

    public int TypeId { get; set; }
    public int LocationId { get; set; }

    public string TypeName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;

    public ICollection<EventLecturerViewModel> EventLecturers { get; set; } = new List<EventLecturerViewModel>();
}
