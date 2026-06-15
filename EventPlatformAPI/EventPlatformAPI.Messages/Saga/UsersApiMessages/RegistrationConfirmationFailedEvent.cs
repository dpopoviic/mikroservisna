namespace EventPlatformAPI.Messages.Saga.UserApiMessages
{
    public class RegistrationConfirmationFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public Guid UserId { get; set; }
        public Guid EventId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
