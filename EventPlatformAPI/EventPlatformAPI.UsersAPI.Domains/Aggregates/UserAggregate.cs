using EventPlatformAPI.UsersAPI.Domains.Events;
using static EventPlatformAPI.UsersAPI.Domains.Events.UserDomainEvent;

namespace EventPlatformAPI.UsersAPI.Domains.Aggregates
{
    public class UserAggregate
    {
        public Guid Id { get; private set; }
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }
        public List<Registration> Registrations { get; private set; } = new();

        public int Version { get; private set; }

        private readonly List<DomainEvent> _uncommittedEvents = new();
        private UserAggregate() { }

        public static UserAggregate Create(
            Guid id,
            string firstName,
            string lastName,
            string email,
            Guid correlationId)
        {
            var aggregate = new UserAggregate();

            aggregate.RaiseEvent(new UserCreatedEvent
            {
                AggregateId = id,
                CorrelationId = correlationId,
                OccurredOn = DateTime.UtcNow,
                FirstName = firstName,
                LastName = lastName,
                Email = email
            });

            return aggregate;
        }
        public void ChangeEmail(string newEmail, Guid correlationId)
        {
            if (string.IsNullOrWhiteSpace(newEmail))
                throw new InvalidOperationException("Email cannot be empty.");

            if (Email == newEmail)
                throw new InvalidOperationException("New email is the same as current email.");

            RaiseEvent(new UserEmailChangedEvent
            {
                AggregateId = Id,
                CorrelationId = correlationId,
                OccurredOn = DateTime.UtcNow,
                OldEmail = Email,
                NewEmail = newEmail
            });
        }

        public void Activate(Guid correlationId)
        {
            if (IsActive)
                throw new InvalidOperationException("User is already active.");

            RaiseEvent(new UserActivatedEvent
            {
                AggregateId = Id,
                CorrelationId = correlationId,
                OccurredOn = DateTime.UtcNow
            });
        }

        public void Deactivate(Guid correlationId)
        {
            if (!IsActive)
                throw new InvalidOperationException("User is already inactive.");

            RaiseEvent(new UserDeactivatedEvent
            {
                AggregateId = Id,
                CorrelationId = correlationId,
                OccurredOn = DateTime.UtcNow
            });
        }

        public void CreateRegistration(Guid eventId, Guid correlationId)
        {
            if (!IsActive)
                throw new InvalidOperationException("Inactive users cannot create registrations.");

            var existing = Registrations.FirstOrDefault(r => r.EventId == eventId);
            if (existing != null && existing.Status != RegistrationStatus.Cancelled)
                throw new InvalidOperationException("User already has an active registration for this event.");

            RaiseEvent(new RegistrationCreatedEvent
            {
                AggregateId = Id,
                CorrelationId = correlationId,
                OccurredOn = DateTime.UtcNow,
                EventId = eventId,
                CreatedAt = DateTime.UtcNow
            });
        }

        public void ConfirmRegistration(Guid eventId, Guid correlationId)
        {
            var registration = Registrations.FirstOrDefault(r => r.EventId == eventId)
                ?? throw new InvalidOperationException($"Registration for event {eventId} does not exist.");

            if (registration.Status == RegistrationStatus.Cancelled)
                throw new InvalidOperationException("Cannot confirm a cancelled registration.");

            if (registration.Status == RegistrationStatus.Confirmed)
                throw new InvalidOperationException("Registration is already confirmed.");

            RaiseEvent(new RegistrationConfirmedEvent
            {
                AggregateId = Id,
                CorrelationId = correlationId,
                OccurredOn = DateTime.UtcNow,
                EventId = eventId
            });
        }

        public void CancelRegistration(Guid eventId, Guid correlationId)
        {
            var registration = Registrations.FirstOrDefault(r => r.EventId == eventId)
                ?? throw new InvalidOperationException($"Registration for event {eventId} does not exist.");

            if (registration.Status == RegistrationStatus.Cancelled)
                throw new InvalidOperationException("Registration is already cancelled.");

            RaiseEvent(new RegistrationCancelledEvent
            {
                AggregateId = Id,
                CorrelationId = correlationId,
                OccurredOn = DateTime.UtcNow,
                EventId = eventId
            });
        }
        private void RaiseEvent(DomainEvent @event)
        {
            Apply(@event);
            Version++;
            _uncommittedEvents.Add(@event);
        }
        public void LoadFromHistory(IEnumerable<DomainEvent> history)
        {
            foreach (var @event in history)
            {
                Apply(@event);
                Version++;
            }
        }
        public IReadOnlyList<DomainEvent> DequeueUncommittedEvents()
        {
            var events = _uncommittedEvents.ToList();
            _uncommittedEvents.Clear();
            return events;
        }
        public void RestoreFromSnapshot(UserAggregateSnapshot snapshot)
        {
            Id = snapshot.Id;
            FirstName = snapshot.FirstName;
            LastName = snapshot.LastName;
            Email = snapshot.Email;
            IsActive = snapshot.IsActive;
            Version = snapshot.Version;
            Registrations = snapshot.Registrations
                .Select(r =>
                {
                    var reg = Registration.Create(r.EventId, r.CreatedAt);
                    if (r.Status == RegistrationStatus.Confirmed) reg.Confirm();
                    if (r.Status == RegistrationStatus.Cancelled) reg.Cancel();
                    return reg;
                })
                .ToList();
        }
        public UserAggregateSnapshot CreateSnapshot()
        {
            return new UserAggregateSnapshot
            {
                Id = Id,
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                IsActive = IsActive,
                Version = Version,
                Registrations = Registrations
                    .Select(r => new RegistrationSnapshot
                    {
                        EventId = r.EventId,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt
                    })
                    .ToList()
            };
        }

        private void Apply(DomainEvent @event)
        {
            switch (@event)
            {
                case UserCreatedEvent e:
                    Id = e.AggregateId;
                    FirstName = e.FirstName;
                    LastName = e.LastName;
                    Email = e.Email;
                    IsActive = true;
                    Registrations = new List<Registration>();
                    break;

                case UserEmailChangedEvent e:
                    Email = e.NewEmail;
                    break;

                case UserActivatedEvent:
                    IsActive = true;
                    break;

                case UserDeactivatedEvent:
                    IsActive = false;
                    break;

                case RegistrationCreatedEvent e:
                    Registrations.Add(Registration.Create(e.EventId, e.CreatedAt));
                    break;

                case RegistrationConfirmedEvent e:
                    Registrations.First(r => r.EventId == e.EventId).Confirm();
                    break;

                case RegistrationCancelledEvent e:
                    Registrations.First(r => r.EventId == e.EventId).Cancel();
                    break;

                default:
                    throw new InvalidOperationException($"Unknown domain event type: {@event.GetType().Name}");
            }
        }
    }
}
