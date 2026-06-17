namespace EventPlatformAPI.Messages.Saga.UserApiMessages
{
    public class RegistrationCancelledEvent
    {
        public Guid CorrelationId { get; set; }
        public Guid UserId { get; set; }
        public int EventId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
