using System;
using System.Collections.Generic;

namespace EventPlatformAPI.Messages.Requests
{
    public class ValidateReferencesRequest
    {
        public Guid CorrelationId { get; set; }
        public int? LocationId { get; set; }
        public List<int> LecturerIds { get; set; } = new List<int>();
        //potencijalno samo jedan lecturer.    
    }
}
