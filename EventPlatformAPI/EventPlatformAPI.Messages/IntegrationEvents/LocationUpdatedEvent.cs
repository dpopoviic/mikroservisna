using System;

namespace EventPlatformAPI.Messages.IntegrationEvents
{
    public class LocationUpdatedEvent
    {
        public Guid EventId { get; set; }
        public DateTime OccurredAt { get; set; }
        public int LocationId { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public int? Capacity { get; set; }
    }
}
