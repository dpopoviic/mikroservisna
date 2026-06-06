using System;

namespace EventPlatformAPI.Messages.Requests
{
    public class ValidateReferencesResponse
    {
        public Guid CorrelationId { get; set; }
        public bool IsValid { get; set; }
        public string? Reason { get; set; }
    }
}
