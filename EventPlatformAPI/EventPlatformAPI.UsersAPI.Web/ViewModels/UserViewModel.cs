using System.ComponentModel.DataAnnotations;

namespace EventPlatformAPI.UsersAPI.Web.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        [MaxLength(200)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(500)]
        public string Email { get; set; } = string.Empty;
    }
    public class UpdateUserEmailViewModel
    {
        [Required]
        [EmailAddress]
        [MaxLength(500)]
        public string NewEmail { get; set; } = string.Empty;
    }

}
