using EventPlatformAPI.EventsAPI.Data;
using EventPlatformAPI.EventsAPI.Models;
using EventPlatformAPI.Messages.IntegrationEvents;
using System.Text.Json;

namespace EventPlatformAPI.EventsAPI.HostedServices;

public interface IEventSnapshotPublisher
{
    void Enqueue(EventsDbContext db, int eventId, bool isPublished, DateTime eventDate);
}

public class EventSnapshotPublisher : IEventSnapshotPublisher
{
    public void Enqueue(EventsDbContext db, int eventId, bool isPublished, DateTime eventDate)
    {
        var @event = new EventPublishedSnapshotEvent
        {
            CorrelationId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            EventId = eventId,
            IsPublished = isPublished,
            EventDate = eventDate
        };

        db.OutboxMessages.Add(new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Destination = "references.event-snapshot.event",
            Type = nameof(EventPublishedSnapshotEvent),
            Payload = JsonSerializer.Serialize(@event),
            CreatedAtUtc = DateTime.UtcNow,
            IsPublished = false
        });
    }
}
