using System;

namespace EventPlatformAPI.Messages.IntegrationEvents
{
    public class LocationDeletedEvent
    {
        public Guid EventId { get; set; }
        public DateTime OccurredAt { get; set; }
        public int LocationId { get; set; }
    }
}
