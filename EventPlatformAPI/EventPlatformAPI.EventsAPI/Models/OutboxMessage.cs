using System;

namespace EventPlatformAPI.EventsAPI.Models
{
    public class OutboxMessage
    {
        public long Id { get; set; }
        public Guid MessageId { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
       public Guid CorrelationId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? PublishedAtUtc { get; set; }
        public bool IsPublished { get; set; }
    }
}
