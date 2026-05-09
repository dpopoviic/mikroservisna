using System;

namespace EventPlatformAPI.Messages.IntegrationEvents
{
    public class LocationCreatedEvent
    {
        public Guid EventId { get; set; }
        public DateTime OccurredAt { get; set; }
        public int LocationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int Capacity { get; set; }
    }
}
