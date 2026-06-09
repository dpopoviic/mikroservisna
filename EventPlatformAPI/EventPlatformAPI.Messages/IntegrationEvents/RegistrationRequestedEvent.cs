using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatformAPI.Messages.IntegrationEvents
{
    public class RegistrationRequestedEvent
    {
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; }
        public Guid RegistrationId { get; set; }
        public int EventId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
    }

}
