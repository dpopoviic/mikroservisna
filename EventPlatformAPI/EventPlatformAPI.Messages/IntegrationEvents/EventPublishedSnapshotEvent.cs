namespace EventPlatformAPI.Messages.IntegrationEvents
{
    public class EventPublishedSnapshotEvent
    {
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; }
        public int EventId { get; set; }
        public bool IsPublished { get; set; }
        public DateTime EventDate { get; set; }
    }
}
