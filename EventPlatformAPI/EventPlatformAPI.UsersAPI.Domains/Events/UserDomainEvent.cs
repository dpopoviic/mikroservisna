using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatformAPI.UsersAPI.Domains.Events
{
    public class UserDomainEvent
    {
        public class UserCreatedEvent : DomainEvent
        {
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public class UserEmailChangedEvent : DomainEvent
        {
            public string OldEmail { get; set; } = string.Empty;
            public string NewEmail { get; set; } = string.Empty;
        }

        public class UserActivatedEvent : DomainEvent { }

        public class UserDeactivatedEvent : DomainEvent { }

        public class RegistrationCreatedEvent : DomainEvent
        {
            public Guid EventId { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class RegistrationConfirmedEvent : DomainEvent
        {
            public Guid EventId { get; set; }
        }

        public class RegistrationCancelledEvent : DomainEvent
        {
            public Guid EventId { get; set; }
        }
    }
}
