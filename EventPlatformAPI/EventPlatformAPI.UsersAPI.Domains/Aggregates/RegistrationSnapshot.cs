using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatformAPI.UsersAPI.Domains.Aggregates
{
    public class RegistrationSnapshot
    {
        public Guid EventId { get; set; }
        public RegistrationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
