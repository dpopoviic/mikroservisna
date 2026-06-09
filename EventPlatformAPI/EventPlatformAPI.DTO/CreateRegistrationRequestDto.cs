using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatformAPI.DTO
{
    public class CreateRegistrationRequestDto
    {
        public int EventId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
    }
}
