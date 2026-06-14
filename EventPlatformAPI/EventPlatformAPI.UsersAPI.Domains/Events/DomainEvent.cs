namespace EventPlatformAPI.UsersAPI.Domains.Events
{
    public abstract class DomainEvent
    {
        public Guid AggregateId { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTime OccurredOn { get; set; }
    }
}
