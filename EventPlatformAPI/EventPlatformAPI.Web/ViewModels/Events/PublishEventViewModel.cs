using System.ComponentModel.DataAnnotations;

namespace EventPlatformAPI.Web.ViewModels.Events
{
    public class PublishEventViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Agenda { get; set; } = string.Empty;

        [Required]
        public DateTime DateTime { get; set; } = DateTime.Now.AddDays(7);

        [Range(0.5, 24)]
        public decimal DurationInHours { get; set; }

        [Range(0, 99999)]
        public decimal Price { get; set; }

        [Required]
        public int TypeId { get; set; }

        [Required]
        public int LocationId { get; set; }

        public List<int> SelectedLecturerIds { get; set; } = [];

        [EmailAddress]
        public string OrganizerEmail { get; set; } = string.Empty;
    }
}
