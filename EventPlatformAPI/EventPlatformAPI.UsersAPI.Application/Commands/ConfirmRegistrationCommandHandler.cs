using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.Messages.Saga;
using EventPlatformAPI.Messages.Saga.UserApiMessages;
using EventPlatformAPI.UsersAPI.Domains.Outbox;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public class ConfirmRegistrationCommandHandler : ICommandHandler<ConfirmRegistrationCommand>
    {
        private readonly IUserWriteRepository _repository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly ILogger<ConfirmRegistrationCommandHandler> _logger;

        public ConfirmRegistrationCommandHandler(
            IUserWriteRepository repository,
            IOutboxRepository outboxRepository,
            ILogger<ConfirmRegistrationCommandHandler> logger)
        {
            _repository = repository;
            _outboxRepository = outboxRepository;
            _logger = logger;
        }

        public async Task HandleAsync(
            ConfirmRegistrationCommand command,
            CancellationToken cancellationToken = default)
        {
            var aggregate = await _repository.LoadAsync(command.UserId, cancellationToken);
            if (aggregate is null)
                throw new Exception("Ne postoji traženi korisnik.");

            try
            {
                aggregate.ConfirmRegistration(command.EventId, command.CorrelationId);
                await _repository.SaveAsync(aggregate, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                var failedEvent = new RegistrationConfirmationFailedEvent
                {
                    CorrelationId = command.CorrelationId,
                    UserId = command.UserId,
                    EventId = command.EventId,
                    Reason = ex.Message,
                    Timestamp = DateTime.UtcNow
                };

                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    CorrelationId = command.CorrelationId,
                    Type = nameof(RegistrationConfirmationFailedEvent),
                    Destination = SagaQueues.RegistrationConfirmationFailed,
                    Payload = JsonSerializer.Serialize(failedEvent),
                    CreatedAt = DateTime.UtcNow,
                    IsPublished = false
                };

                await _outboxRepository.AddAsync(outboxMessage, cancellationToken);

                _logger.LogWarning(
                    "[CorrelationId={CorrelationId}] RegistrationConfirmationFailed: {Reason}",
                    command.CorrelationId, ex.Message);

                throw new Exception($"Greška prilikom potvrde registracije: {ex.Message}", ex);
            }
        }
    }
}
