using System;

namespace EventPlatformAPI.ReferencesAPI.Models
{
    public class OutboxMessage
    {
        public long Id { get; set; }
        public Guid MessageId { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool IsPublished { get; set; }
    }
}
