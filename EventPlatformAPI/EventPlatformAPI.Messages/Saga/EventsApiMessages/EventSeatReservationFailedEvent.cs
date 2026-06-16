namespace EventPlatformAPI.Messages.Saga.EventsApiMessages
{
    public class EventSeatReservationFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public int EventId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
