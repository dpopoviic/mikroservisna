namespace EventPlatformAPI.Messages.Saga.EventsApiMessages
{
    public class RegistrationEmailSentEvent
    {
        public Guid CorrelationId { get; set; }
        public Guid UserId { get; set; }
        public int EventId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
