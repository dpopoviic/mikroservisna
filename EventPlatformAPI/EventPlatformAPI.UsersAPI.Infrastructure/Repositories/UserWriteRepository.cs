using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Domains.Aggregates;
using EventPlatformAPI.UsersAPI.Domains.Entities;
using EventPlatformAPI.UsersAPI.Domains.Events;
using EventPlatformAPI.UsersAPI.Infrastructure.Data;
using EventPlatformAPI.UsersAPI.Infrastructure.Projectors;
using EventPlatformAPI.UsersAPI.Infrastructure.Snapshots;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static EventPlatformAPI.UsersAPI.Domains.Events.UserDomainEvent;
using EventPlatformAPI.Messages.Saga;
using SagaMessages = EventPlatformAPI.Messages.Saga.UserApiMessages;
using EventPlatformAPI.UsersAPI.Domains.Outbox;

namespace EventPlatformAPI.UsersAPI.Infrastructure.Repositories
{
    public class UserWriteRepository(UsersDbContext db, UserProjector projector) : IUserWriteRepository
    {
        private const int SnapshotThreshold = 5;
        private const string AggregateTypeName = "UserAggregate";


        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly Dictionary<string, Type> EventTypeMap = new()
        {
            [nameof(UserCreatedEvent)] = typeof(UserCreatedEvent),
            [nameof(UserEmailChangedEvent)] = typeof(UserEmailChangedEvent),
            [nameof(UserActivatedEvent)] = typeof(UserActivatedEvent),
            [nameof(UserDeactivatedEvent)] = typeof(UserDeactivatedEvent),
            [nameof(RegistrationCreatedEvent)] = typeof(RegistrationCreatedEvent),
            [nameof(RegistrationConfirmedEvent)] = typeof(RegistrationConfirmedEvent),
            [nameof(RegistrationCancelledEvent)] = typeof(RegistrationCancelledEvent),
        };

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            return await db.Users
                            .AsNoTracking()
                            .AnyAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<UserAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var aggregate = UserAggregate.Reconstruct();

            var snapshotRow = await db.Snapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.AggregateId == id, cancellationToken);

            int startFromVersion = 0;

            if (snapshotRow is not null)
            {
                var snapshotData = JsonSerializer.Deserialize<UserAggregateSnapshot>(
                    snapshotRow.SnapshotData, JsonOptions)
                    ?? throw new InvalidOperationException(
                        $"Failed to deserialize snapshot for aggregate {id}.");

                aggregate.RestoreFromSnapshot(snapshotData);
                startFromVersion = snapshotRow.Version;
            }

            var records = await db.EventStore
                .AsNoTracking()
                .Where(e => e.AggregateId == id && e.Version > startFromVersion)
                .OrderBy(e => e.Version)
                .ToListAsync(cancellationToken);

            if (startFromVersion == 0 && records.Count == 0)
                return null;

            var domainEvents = records.Select(DeserializeEvent).ToList();
            aggregate.LoadFromHistory(domainEvents);

            return aggregate;
        }

        public async Task SaveAsync(UserAggregate user, CancellationToken cancellationToken = default)
        {
            var uncommitted = user.DequeueUncommittedEvents();
            if (uncommitted.Count == 0) return;

            int expectedVersion = user.Version - uncommitted.Count;

            var currentMaxVersion = await db.EventStore
                .Where(e => e.AggregateId == user.Id)
                .Select(e => (int?)e.Version)
                .MaxAsync(cancellationToken) ?? 0;

            if (currentMaxVersion != expectedVersion)
                throw new InvalidOperationException(
                    $"Optimistic concurrency conflict for aggregate {user.Id}. " +
                    $"Expected version {expectedVersion}, current is {currentMaxVersion}.");

            int version = expectedVersion;
            foreach (var domainEvent in uncommitted)
            {
                version++;
                db.EventStore.Add(new EventStoreRecord
                {
                    Id = Guid.NewGuid(),
                    AggregateId = user.Id,
                    AggregateType = AggregateTypeName,
                    EventType = domainEvent.GetType().Name,
                    EventData = JsonSerializer.Serialize(
                        domainEvent, domainEvent.GetType(), JsonOptions),
                    Version = version,
                    CorrelationId = domainEvent.CorrelationId,
                    OccurredOn = domainEvent.OccurredOn
                });
            }

            int existingCount = await db.EventStore
                .CountAsync(e => e.AggregateId == user.Id, cancellationToken);

            int newTotal = existingCount + uncommitted.Count;
            if (newTotal % SnapshotThreshold == 0)
                await UpsertSnapshotAsync(user, cancellationToken);

           foreach (var domainEvent in uncommitted)
          {
               await projector.ProjectAsync(domainEvent, cancellationToken);
              GenerateSagaOutboxMessages(user, domainEvent);
          }

            await db.SaveChangesAsync(cancellationToken);
        }
        private async Task UpsertSnapshotAsync(
            UserAggregate aggregate,
            CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(aggregate.CreateSnapshot(), JsonOptions);

            var existing = await db.Snapshots
                .FirstOrDefaultAsync(s => s.AggregateId == aggregate.Id, cancellationToken);

            if (existing is not null)
            {
                existing.SnapshotData = json;
                existing.Version = aggregate.Version;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                db.Snapshots.Add(new Snapshot
                {
                    AggregateId = aggregate.Id,
                    AggregateType = AggregateTypeName,
                    SnapshotData = json,
                    Version = aggregate.Version,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        private void GenerateSagaOutboxMessages(UserAggregate user, DomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case RegistrationCreatedEvent e:
                    db.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        CorrelationId = e.CorrelationId,
                        Type = nameof(SagaMessages.RegistrationRequestedEvent),
                        Destination = SagaQueues.RegistrationRequested,
                        Payload = JsonSerializer.Serialize(new SagaMessages.RegistrationRequestedEvent
                        {
                            CorrelationId = e.CorrelationId,
                            RegistrationId = Guid.NewGuid(),
                            UserId = user.Id,
                            EventId = e.EventId,
                            UserEmail = user.Email,
                            UserFirstName = user.FirstName,
                            UserLastName = user.LastName,
                            Timestamp = e.OccurredOn
                        }, JsonOptions),
                        CreatedAt = DateTime.UtcNow,
                        IsPublished = false
                    });
                    break;

                case RegistrationConfirmedEvent e:
                    db.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        CorrelationId = e.CorrelationId,
                        Type = nameof(SagaMessages.RegistrationConfirmedEvent),
                        Destination = SagaQueues.RegistrationConfirmed,
                        Payload = JsonSerializer.Serialize(new SagaMessages.RegistrationConfirmedEvent
                        {
                            CorrelationId = e.CorrelationId,
                            RegistrationId = Guid.NewGuid(),
                            UserId = user.Id,
                            EventId = e.EventId,
                            Timestamp = e.OccurredOn
                        }, JsonOptions),
                        CreatedAt = DateTime.UtcNow,
                        IsPublished = false
                    });
                    break;

                case RegistrationCancelledEvent e:
                    db.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        CorrelationId = e.CorrelationId,
                        Type = nameof(SagaMessages.RegistrationCancelledEvent),
                        Destination = SagaQueues.RegistrationCancelled,
                        Payload = JsonSerializer.Serialize(new SagaMessages.RegistrationCancelledEvent
                        {
                            CorrelationId = e.CorrelationId,
                            UserId = user.Id,
                            EventId = e.EventId,
                            Timestamp = e.OccurredOn
                        }, JsonOptions),
                        CreatedAt = DateTime.UtcNow,
                        IsPublished = false
                    });
                    break;
            }
        }

       private static DomainEvent DeserializeEvent(EventStoreRecord record)
        {
            if (!EventTypeMap.TryGetValue(record.EventType, out var eventType))
                throw new InvalidOperationException(
                    $"Unknown event type '{record.EventType}' in EventStore.");

            return (DomainEvent?)JsonSerializer.Deserialize(
                record.EventData, eventType, JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Deserialization returned null for event type '{record.EventType}'.");
        }
    }
}
