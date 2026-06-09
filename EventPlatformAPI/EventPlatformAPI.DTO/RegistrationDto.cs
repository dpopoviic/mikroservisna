using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatformAPI.DTO
{
    public class RegistrationDto
    {
        public Guid Id { get; set; }
        public int EventId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid CorrelationId { get; set; }
    }
}
