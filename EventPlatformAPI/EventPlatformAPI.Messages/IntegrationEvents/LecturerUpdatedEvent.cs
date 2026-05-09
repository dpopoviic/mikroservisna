using System;

namespace EventPlatformAPI.Messages.IntegrationEvents
{
    public class LecturerUpdatedEvent
    {
        public Guid EventId { get; set; }
        public DateTime OccurredAt { get; set; }
        public int LecturerId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Title { get; set; }
    }
}
