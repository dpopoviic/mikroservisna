using EventPlatformAPI.DTO;
using EventPlatformAPI.EventsAPI.Data;
using EventPlatformAPI.EventsAPI.Models;
using EventPlatformAPI.Messages.IntegrationEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EventPlatformAPI.EventsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationsController : ControllerBase
{
    private readonly EventsDbContext _db;
    private readonly ILogger<RegistrationsController> _logger;

    public RegistrationsController(EventsDbContext db, ILogger<RegistrationsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new event registration and kicks off the Saga Choreography.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRegistration(
        [FromBody] CreateRegistrationRequestDto request,
        CancellationToken ct)
    {
        var correlationId = Guid.NewGuid();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            // 1. Create Registration entity
            var registration = new Registration
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                ParticipantName = request.ParticipantName,
                ParticipantEmail = request.ParticipantEmail,
                Status = RegistrationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.Registrations.Add(registration);

            // 2. Create initial saga state
            var sagaState = new RegistrationSagaState
            {
                Id = Guid.NewGuid(),
                CorrelationId = correlationId,
                RegistrationId = registration.Id,
                CurrentState = "Requested",
                StartedAt = DateTime.UtcNow
            };

            _db.RegistrationSagaStates.Add(sagaState);

            // 3. Enqueue RegistrationRequestedEvent via Outbox
            var @event = new RegistrationRequestedEvent
            {
                CorrelationId = correlationId,
                OccurredAt = DateTime.UtcNow,
                RegistrationId = registration.Id,
                EventId = request.EventId,
                ParticipantName = request.ParticipantName,
                ParticipantEmail = request.ParticipantEmail
            };

            _db.OutboxMessages.Add(new OutboxMessage
            {
                MessageId = Guid.NewGuid(),
                Destination = "registration.requested.event",
                Type = nameof(RegistrationRequestedEvent),
                Payload = JsonSerializer.Serialize(@event),
                CreatedAtUtc = DateTime.UtcNow,
                IsPublished = false
            });

            // 4. Log saga event
            _db.SagaEventLogs.Add(new SagaEventLog
            {
                Id = Guid.NewGuid(),
                CorrelationId = correlationId,
                EventName = nameof(RegistrationRequestedEvent),
                Payload = JsonSerializer.Serialize(@event),
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "[EVENTSAPI] Registration {RegistrationId} created. CorrelationId={CorrelationId}",
                registration.Id, correlationId);

            return Accepted(new RegistrationDto
            {
                Id = registration.Id,
                EventId = registration.EventId,
                ParticipantName = registration.ParticipantName,
                ParticipantEmail = registration.ParticipantEmail,
                Status = registration.Status.ToString(),
                CreatedAt = registration.CreatedAt,
                CorrelationId = correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENTSAPI] Failed to create registration for EventId={EventId}", request.EventId);
            await tx.RollbackAsync(ct);
            return StatusCode(500, "Failed to create registration.");
        }
    }
}
