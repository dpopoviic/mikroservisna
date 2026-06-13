using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatformAPI.UsersAPI.Domains.Events
{
    public abstract class DomainEvent
    {
        public Guid AggregateId { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTime OccurredOn { get; set; }
    }
}
