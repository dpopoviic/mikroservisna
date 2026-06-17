namespace EventPlatformAPI.Messages.Saga.EventsApiMessages
{
    public class EventSeatReservedEvent
    {
        public Guid CorrelationId { get; set; }
        public int EventId { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
