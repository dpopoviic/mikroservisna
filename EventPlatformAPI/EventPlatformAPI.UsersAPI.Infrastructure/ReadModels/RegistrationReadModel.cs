namespace EventPlatformAPI.UsersAPI.Infrastructure.ReadModels
{
    public class RegistrationReadModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int EventId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string UserFirstName { get; set; } = string.Empty;
        public string UserLastName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }
}
