using System;

namespace EventPlatformAPI.Messages.IntegrationEvents
{
    public class LecturerCreatedEvent
    {
        public Guid EventId { get; set; }
        public DateTime OccurredAt { get; set; }
        public int LecturerId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Title { get; set; }
    }
}
