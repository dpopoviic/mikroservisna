namespace EventPlatformAPI.Messages.Saga.SagaMessages
{
    public class ConfirmRegistrationSagaCommand
    {
        public Guid CorrelationId { get; set; }
        public Guid UserId { get; set; }
        public Guid EventId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
