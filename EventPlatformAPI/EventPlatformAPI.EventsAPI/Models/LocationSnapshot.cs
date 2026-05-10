using System;

namespace EventPlatformAPI.EventsAPI.Models
{
    public class LocationSnapshot
    {
        public int Id { get; set; }
        public int ExternalId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int Capacity { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
