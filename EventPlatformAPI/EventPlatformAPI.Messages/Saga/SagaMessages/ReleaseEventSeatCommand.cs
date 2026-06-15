namespace EventPlatformAPI.Messages.Saga.SagaMessages
{
    public class ReleaseEventSeatCommand
    {
        public Guid CorrelationId { get; set; }
        public Guid EventId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
