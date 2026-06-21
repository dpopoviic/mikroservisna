using EventPlatformAPI.Messages.Saga;
using EventPlatformAPI.Messages.Saga.EventsApiMessages;
using EventPlatformAPI.Messages.Saga.SagaMessages;
using EventPlatformAPI.Messages.Saga.UserApiMessages;
using EventPlatformAPI.SagaOrcgestrator.Data;
using EventPlatformAPI.SagaOrcgestrator.Entities;
using EventPlatformAPI.SagaOrcgestrator.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventPlatformAPI.SagaOrcgestrator.Services
{
    public class RegistrationSagaCoordinator(
            IRegistrationSagaStateRepository repository,
            SagaDbContext db,
            ILogger<RegistrationSagaCoordinator> logger)
            : IRegistrationSagaCoordinator
    {
        public async Task HandleRegistrationRequestedAsync(
          RegistrationRequestedEvent evt, CancellationToken ct = default)
        {
            logger.LogInformation(
                "[CorrelationId={CorrelationId}] Saga received RegistrationRequested. UserId={UserId} EventId={EventId}",
                evt.CorrelationId, evt.UserId, evt.EventId);

            var existing = await repository.FindByCorrelationIdAsync(evt.CorrelationId, ct);
            if (existing is not null)
            {
                logger.LogWarning(
                    "[CorrelationId={CorrelationId}] Saga already exists (Status={Status}). Ignoring duplicate.",
                    evt.CorrelationId, existing.Status);
                return;
            }

            var sagaId = Guid.NewGuid();

            var state = new RegistrationSagaState
            {
                Id = sagaId,
                CorrelationId = evt.CorrelationId,
                UserId = evt.UserId,
                EventId = evt.EventId,
                UserEmail = evt.UserEmail,
                UserFirstName = evt.UserFirstName,
                UserLastName = evt.UserLastName,
                Status = RegistrationSagaStatus.Started,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var command = new ReserveEventSeatCommand
            {
                CorrelationId = evt.CorrelationId,
                EventId = evt.EventId,
                UserId = evt.UserId,
                Timestamp = DateTime.UtcNow
            };

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            db.RegistrationSagaStates.Add(state);
            EnqueueOutbox(evt.CorrelationId, SagaQueues.ReserveEventSeat, command);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            LogTransition(evt.CorrelationId,
                RegistrationSagaStatus.Started,
                RegistrationSagaStatus.WaitingForSeatReservation);

            state.Status = RegistrationSagaStatus.WaitingForSeatReservation;
            state.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(state, ct);
        }
        public async Task HandleEventSeatReservedAsync(
          EventSeatReservedEvent evt, CancellationToken ct = default)
        {
            logger.LogInformation(
                "[CorrelationId={CorrelationId}] EventSeatReserved received.",
                evt.CorrelationId);

            var state = await GetStateOrWarnAsync(evt.CorrelationId, ct);
            if (state is null) return;

            var command = new ConfirmRegistrationSagaCommand
            {
                CorrelationId = evt.CorrelationId,
                UserId = state.UserId,
                EventId = state.EventId,
                Timestamp = DateTime.UtcNow
            };

            await TransitionAsync(state,
                RegistrationSagaStatus.WaitingForRegistrationConfirmation,
                SagaQueues.ConfirmRegistration, command, ct);
        }
        public async Task HandleEventSeatReservationFailedAsync(
                    EventSeatReservationFailedEvent evt, CancellationToken ct = default)
        {
            logger.LogWarning(
                "[CorrelationId={CorrelationId}] EventSeatReservationFailed: {Reason}",
                evt.CorrelationId, evt.Reason);

            var state = await GetStateOrWarnAsync(evt.CorrelationId, ct);
            if (state is null) return;

            var cancelCmd = new CancelRegistrationSagaCommand
            {
                CorrelationId = evt.CorrelationId,
                UserId = state.UserId,
                EventId = state.EventId,
                Reason = evt.Reason,
                Timestamp = DateTime.UtcNow
            };

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            EnqueueOutbox(evt.CorrelationId, SagaQueues.CancelRegistration, cancelCmd);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            LogTransition(evt.CorrelationId, state.Status, RegistrationSagaStatus.Failed);
            logger.LogWarning("[CorrelationId={CorrelationId}] Compensation: CancelRegistration sent.", evt.CorrelationId);

            state.Status = RegistrationSagaStatus.Failed;
            state.FailureReason = evt.Reason;
            state.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(state, ct);
        }
        public async Task HandleRegistrationConfirmedAsync(
         RegistrationConfirmedEvent evt, CancellationToken ct = default)
        {
            logger.LogInformation(
                "[CorrelationId={CorrelationId}] RegistrationConfirmed received.",
                evt.CorrelationId);

            var state = await GetStateOrWarnAsync(evt.CorrelationId, ct);
            if (state is null) return;

            var command = new SendRegistrationEmailCommand
            {
                CorrelationId = evt.CorrelationId,
                UserId = state.UserId,
                EventId = state.EventId,
                UserEmail = state.UserEmail,
                UserFullName = $"{state.UserFirstName} {state.UserLastName}",
                Timestamp = DateTime.UtcNow
            };

            await TransitionAsync(state,
                RegistrationSagaStatus.WaitingForEmail,
                SagaQueues.SendRegistrationEmail, command, ct);
        }
        public async Task HandleRegistrationConfirmationFailedAsync(
          RegistrationConfirmationFailedEvent evt, CancellationToken ct = default)
        {
            logger.LogWarning(
                "[CorrelationId={CorrelationId}] RegistrationConfirmationFailed: {Reason}. Starting compensation.",
                evt.CorrelationId, evt.Reason);

            var state = await GetStateOrWarnAsync(evt.CorrelationId, ct);
            if (state is null) return;

            var releaseCmd = new ReleaseEventSeatCommand
            {
                CorrelationId = evt.CorrelationId,
                EventId = state.EventId,
                UserId = state.UserId,
                Timestamp = DateTime.UtcNow
            };

            var cancelCmd = new CancelRegistrationSagaCommand
            {
                CorrelationId = evt.CorrelationId,
                UserId = state.UserId,
                EventId = state.EventId,
                Reason = evt.Reason,
                Timestamp = DateTime.UtcNow
            };

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            LogTransition(evt.CorrelationId, state.Status, RegistrationSagaStatus.Compensating);
            state.Status = RegistrationSagaStatus.Compensating;
            state.FailureReason = evt.Reason;
            state.UpdatedAt = DateTime.UtcNow;
            db.RegistrationSagaStates.Update(state);

            EnqueueOutbox(evt.CorrelationId, SagaQueues.ReleaseEventSeat, releaseCmd);
            EnqueueOutbox(evt.CorrelationId, SagaQueues.CancelRegistration, cancelCmd);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            logger.LogWarning(
                "[CorrelationId={CorrelationId}] Compensation: ReleaseEventSeat + CancelRegistration sent.",
                evt.CorrelationId);

            LogTransition(evt.CorrelationId, RegistrationSagaStatus.Compensating, RegistrationSagaStatus.Failed);
            state.Status = RegistrationSagaStatus.Failed;
            state.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(state, ct);
        }
        public async Task HandleRegistrationEmailSentAsync(
          RegistrationEmailSentEvent evt, CancellationToken ct = default)
        {
            logger.LogInformation(
                "[CorrelationId={CorrelationId}] RegistrationEmailSent received. Completing saga.",
                evt.CorrelationId);

            var state = await GetStateOrWarnAsync(evt.CorrelationId, ct);
            if (state is null) return;

            LogTransition(evt.CorrelationId, state.Status, RegistrationSagaStatus.Completed);
            state.Status = RegistrationSagaStatus.Completed;
            state.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(state, ct);

            logger.LogInformation(
                "[CorrelationId={CorrelationId}] Saga COMPLETED successfully.",
                evt.CorrelationId);
        }
        public async Task HandleRegistrationEmailFailedAsync(
           RegistrationEmailFailedEvent evt, CancellationToken ct = default)
        {
            logger.LogWarning(
                "[CorrelationId={CorrelationId}] RegistrationEmailFailed: {Reason}. Starting compensation.",
                evt.CorrelationId, evt.Reason);

            var state = await GetStateOrWarnAsync(evt.CorrelationId, ct);
            if (state is null) return;

            var releaseCmd = new ReleaseEventSeatCommand
            {
                CorrelationId = evt.CorrelationId,
                EventId = state.EventId,
                UserId = state.UserId,
                Timestamp = DateTime.UtcNow
            };

            var cancelCmd = new CancelRegistrationSagaCommand
            {
                CorrelationId = evt.CorrelationId,
                UserId = state.UserId,
                EventId = state.EventId,
                Reason = evt.Reason,
                Timestamp = DateTime.UtcNow
            };

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            LogTransition(evt.CorrelationId, state.Status, RegistrationSagaStatus.Compensating);
            state.Status = RegistrationSagaStatus.Compensating;
            state.FailureReason = evt.Reason;
            state.UpdatedAt = DateTime.UtcNow;
            db.RegistrationSagaStates.Update(state);

            EnqueueOutbox(evt.CorrelationId, SagaQueues.ReleaseEventSeat, releaseCmd);
            EnqueueOutbox(evt.CorrelationId, SagaQueues.CancelRegistration, cancelCmd);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            logger.LogWarning(
                "[CorrelationId={CorrelationId}] Compensation: ReleaseEventSeat + CancelRegistration sent.",
                evt.CorrelationId);

            LogTransition(evt.CorrelationId, RegistrationSagaStatus.Compensating, RegistrationSagaStatus.Failed);
            state.Status = RegistrationSagaStatus.Failed;
            state.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(state, ct);
        }

        private void LogTransition(Guid correlationId,
         RegistrationSagaStatus from, RegistrationSagaStatus to)
        {
            logger.LogInformation(
                "[CorrelationId={CorrelationId}] Saga status changed: {From} -> {To}",
                correlationId, from, to);
        }
        private async Task TransitionAsync<TCmd>(
         RegistrationSagaState state,
         RegistrationSagaStatus nextStatus,
         string destination,
         TCmd command,
         CancellationToken ct) where TCmd : notnull
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            LogTransition(state.CorrelationId, state.Status, nextStatus);
            state.Status = nextStatus;
            state.UpdatedAt = DateTime.UtcNow;
            db.RegistrationSagaStates.Update(state);

            EnqueueOutbox(state.CorrelationId, destination, command);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        private void EnqueueOutbox<TMsg>(Guid correlationId, string destination, TMsg message) where TMsg : notnull
        {
            var outbox = new SagaOutboxMessage
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = correlationId,
                Type = typeof(TMsg).Name,
                Destination = destination,
                Payload = JsonSerializer.Serialize(message),
                CreatedAt = DateTime.UtcNow,
                IsPublished = false
            };

            db.SagaOutboxMessages.Add(outbox);

            logger.LogInformation(
                "[CorrelationId={CorrelationId}] Outbox enqueued: {Type} → {Destination}",
                correlationId, outbox.Type, outbox.Destination);
        }
        private async Task<RegistrationSagaState?> GetStateOrWarnAsync(Guid correlationId, CancellationToken ct)
        {
            var state = await repository.FindByCorrelationIdAsync(correlationId, ct);
            if (state is null)
            {
                logger.LogWarning(
                    "[CorrelationId={CorrelationId}] Saga state not found. Message ignored.",
                    correlationId);
            }
            return state;
        }
    }
}
