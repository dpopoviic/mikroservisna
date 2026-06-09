namespace EventPlatformAPI.ReferencesAPI.Models
{
    /// <summary>
    /// A lightweight snapshot of an Event pushed from EventsAPI when an event is published.
    /// ReferencesAPI uses this to validate registration requests without HTTP calls.
    /// </summary>
    public class EventSnapshot
    {
        public int Id { get; set; }
        public int ExternalEventId { get; set; }
        public bool IsPublished { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
