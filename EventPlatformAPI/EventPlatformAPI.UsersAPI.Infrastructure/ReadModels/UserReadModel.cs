namespace EventPlatformAPI.UsersAPI.Infrastructure.ReadModels
{
    public class UserReadModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int RegistrationsCount { get; set; }
    }
    
}
