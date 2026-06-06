using System;

namespace EventPlatformAPI.Messages.Requests
{
    public class EmailRequestMessage
    {
        public Guid MessageId { get; set; }
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime EnqueuedAt { get; set; }
    }
}
