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
            public int EventId { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class RegistrationConfirmedEvent : DomainEvent
        {
            public int EventId { get; set; }
        }

        public class RegistrationCancelledEvent : DomainEvent
        {
            public int EventId { get; set; }
        }

        public class RegistrationCancellationCompensatedEvent : DomainEvent
        {
            public int EventId { get; set; }
        }
    }
}
