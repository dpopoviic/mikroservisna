using EventPlatformAPI.UsersAPI.Domains.Events;
using EventPlatformAPI.UsersAPI.Infrastructure.Data;
using EventPlatformAPI.UsersAPI.Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;
using static EventPlatformAPI.UsersAPI.Domains.Events.UserDomainEvent;

namespace EventPlatformAPI.UsersAPI.Infrastructure.Projectors
{
    public class UserProjector (UsersDbContext db)
    {

        public async Task ProjectAsync(DomainEvent @event, CancellationToken cancellationToken = default)
        {
            switch (@event)
            {
                case UserCreatedEvent e:
                    await HandleAsync(e, cancellationToken);
                    break;
                case UserEmailChangedEvent e:
                    await HandleAsync(e, cancellationToken);
                    break;
                case UserActivatedEvent e:
                    await HandleAsync(e, cancellationToken);
                    break;
                case UserDeactivatedEvent e:
                    await HandleAsync(e, cancellationToken);
                    break;
                case RegistrationCreatedEvent e:
                    await HandleAsync(e, cancellationToken);
                    break;
                case RegistrationConfirmedEvent e:
                    await HandleAsync(e, cancellationToken);
                    break;
                case RegistrationCancelledEvent e:
                    await HandleAsync(e, cancellationToken);
                    break;
                case RegistrationCancellationCompensatedEvent e:
                    await HandleAsync(e, cancellationToken);
                    break;
            }
        }

        private async Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken)
        {
            var exists = await db.Users.AnyAsync(u => u.Id == @event.AggregateId, cancellationToken);
            if (exists) return;

            db.Users.Add(new UserReadModel
            {
                Id = @event.AggregateId,
                FirstName = @event.FirstName,
                LastName = @event.LastName,
                Email = @event.Email,
                IsActive = true,
                RegistrationsCount = 0
            });
        }

        private async Task HandleAsync(UserEmailChangedEvent @event, CancellationToken cancellationToken)
        {
            var user = await db.Users.FindAsync([@event.AggregateId], cancellationToken);
            if (user is null) return;

            user.Email = @event.NewEmail;

            var registrations = await db.Registrations
                .Where(r => r.UserId == @event.AggregateId)
                .ToListAsync(cancellationToken);

            foreach (var reg in registrations)
                reg.UserEmail = @event.NewEmail;
        }

        private async Task HandleAsync(UserActivatedEvent @event, CancellationToken cancellationToken)
        {
            var user = await db.Users.FindAsync([@event.AggregateId], cancellationToken);
            if (user is null) return;
            user.IsActive = true;
        }

        private async Task HandleAsync(UserDeactivatedEvent @event, CancellationToken cancellationToken)
        {
            var user = await db.Users.FindAsync([@event.AggregateId], cancellationToken);
            if (user is null) return;
            user.IsActive = false;
        }

        private async Task HandleAsync(RegistrationCreatedEvent @event, CancellationToken cancellationToken)
        {
            var exists = await db.Registrations
                .AnyAsync(r => r.UserId == @event.AggregateId && r.EventId == @event.EventId, cancellationToken);
            if (exists) return;

            var user = await db.Users.FindAsync([@event.AggregateId], cancellationToken);

            db.Registrations.Add(new RegistrationReadModel
            {
                Id = Guid.NewGuid(),
                UserId = @event.AggregateId,
                EventId = @event.EventId,
                Status = "Pending",
                CreatedAt = @event.CreatedAt,
                UserFirstName = user?.FirstName ?? string.Empty,
                UserLastName = user?.LastName ?? string.Empty,
                UserEmail = user?.Email ?? string.Empty
            });

            if (user is not null)
                user.RegistrationsCount++;
        }

        private async Task HandleAsync(RegistrationConfirmedEvent @event, CancellationToken cancellationToken)
        {
            var registration = await db.Registrations
                .FirstOrDefaultAsync(
                    r => r.UserId == @event.AggregateId && r.EventId == @event.EventId,
                    cancellationToken);

            if (registration is null) return;

            if (registration.Status == "Confirmed") return;

            registration.Status = "Confirmed";
        }

        private async Task HandleAsync(RegistrationCancelledEvent @event, CancellationToken cancellationToken)
        {
            var registration = await db.Registrations
                .FirstOrDefaultAsync(
                    r => r.UserId == @event.AggregateId && r.EventId == @event.EventId,
                    cancellationToken);

            if (registration is null) return;

            if (registration.Status == "Cancelled") return;

            registration.Status = "Cancelled";

            var user = await db.Users.FindAsync([@event.AggregateId], cancellationToken);
            if (user is not null && user.RegistrationsCount > 0)
                user.RegistrationsCount--;
        }

        private async Task HandleAsync(RegistrationCancellationCompensatedEvent @event, CancellationToken cancellationToken)
        {
            var registration = await db.Registrations
                .FirstOrDefaultAsync(
                    r => r.UserId == @event.AggregateId && r.EventId == @event.EventId,
                    cancellationToken);

            if (registration is null) return;

            if (registration.Status == "Confirmed") return;

            registration.Status = "Confirmed";

            var user = await db.Users.FindAsync([@event.AggregateId], cancellationToken);
            if (user is not null) user.RegistrationsCount++;
        }
    }
}
