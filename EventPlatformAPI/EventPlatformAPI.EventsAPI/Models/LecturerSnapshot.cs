using System;

namespace EventPlatformAPI.EventsAPI.Models
{
    public class LecturerSnapshot
    {
        public int Id { get; set; }
        public int ExternalId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
