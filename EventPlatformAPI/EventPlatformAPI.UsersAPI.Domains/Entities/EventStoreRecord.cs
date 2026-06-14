namespace EventPlatformAPI.UsersAPI.Domains.Entities
{
    public class EventStoreRecord
    {
        public Guid Id { get; set; }

        public Guid AggregateId { get; set; }

        public string AggregateType { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public string EventData { get; set; } = string.Empty;
        public int Version { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTime OccurredOn { get; set; }
    }
}
