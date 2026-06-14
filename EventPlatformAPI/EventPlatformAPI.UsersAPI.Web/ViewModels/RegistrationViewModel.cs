using System.ComponentModel.DataAnnotations;

namespace EventPlatformAPI.UsersAPI.Web.ViewModels
{

    public class CreateRegistrationViewModel
    {
        [Required]
        public Guid EventId { get; set; }
    }
}
