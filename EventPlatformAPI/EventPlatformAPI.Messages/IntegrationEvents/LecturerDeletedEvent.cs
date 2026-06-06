using System;

namespace EventPlatformAPI.Messages.IntegrationEvents
{
    public class LecturerDeletedEvent
    {
        public Guid EventId { get; set; }
        public DateTime OccurredAt { get; set; }
        public int LecturerId { get; set; }
    }
}
