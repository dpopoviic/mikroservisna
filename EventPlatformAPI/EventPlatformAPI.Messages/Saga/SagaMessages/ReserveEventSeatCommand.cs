namespace EventPlatformAPI.Messages.Saga.SagaMessages
{
    public class ReserveEventSeatCommand
    {
        public Guid CorrelationId { get; set; }
        public int EventId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
