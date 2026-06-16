namespace EventPlatformAPI.Messages.Saga.SagaMessages
{
    public class SendRegistrationEmailCommand
    {
        public Guid CorrelationId { get; set; }
        public Guid UserId { get; set; }
        public int EventId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
