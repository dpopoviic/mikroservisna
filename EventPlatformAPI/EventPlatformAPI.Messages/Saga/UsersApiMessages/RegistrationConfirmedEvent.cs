namespace EventPlatformAPI.Messages.Saga.UserApiMessages
{
    public class RegistrationConfirmedEvent
    {
        public Guid CorrelationId { get; set; }
        public Guid RegistrationId { get; set; }
        public Guid UserId { get; set; }
        public Guid EventId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
