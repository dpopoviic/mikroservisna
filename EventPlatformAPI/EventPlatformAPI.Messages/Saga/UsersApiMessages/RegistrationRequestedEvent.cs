namespace EventPlatformAPI.Messages.Saga.UserApiMessages
{
    public class RegistrationRequestedEvent
    {
        public Guid CorrelationId { get; set; }
        public Guid RegistrationId { get; set; }   
        public Guid UserId { get; set; }
        public int EventId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserFirstName { get; set; } = string.Empty;
        public string UserLastName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
