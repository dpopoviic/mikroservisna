using System.ComponentModel.DataAnnotations;

namespace EventPlatformAPI.Web.ViewModels.EventLecturers;

public class EventLecturerViewModel
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int LecturerId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string LecturerFullName { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    [Required(ErrorMessage = "Tema je obavezna.")]
    public string Theme { get; set; } = string.Empty;
}
