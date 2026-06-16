using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.Messages.Saga;
using EventPlatformAPI.Messages.Saga.UserApiMessages;
using EventPlatformAPI.UsersAPI.Domains.Outbox;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public class CancelRegistrationCommandHandler : ICommandHandler<CancelRegistrationCommand>
    {
        private readonly IUserWriteRepository _repository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly ILogger<CancelRegistrationCommandHandler> _logger;

        public CancelRegistrationCommandHandler(
            IUserWriteRepository repository,
            IOutboxRepository outboxRepository,
            ILogger<CancelRegistrationCommandHandler> logger)
        {
            _repository = repository;
            _outboxRepository = outboxRepository;
            _logger = logger;
        }

        public async Task HandleAsync(
            CancelRegistrationCommand command,
            CancellationToken cancellationToken = default)
        {
            var aggregate = await _repository.LoadAsync(command.UserId, cancellationToken);
            if (aggregate is null)
                throw new Exception("Ne postoji traženi korisnik.");

            try
            {
                aggregate.CancelRegistration(command.EventId, command.CorrelationId);
                await _repository.SaveAsync(aggregate, cancellationToken);

                var cancelledEvent = new RegistrationCancelledEvent
                {
                    CorrelationId = command.CorrelationId,
                    UserId = command.UserId,
                    EventId = command.EventId,
                    Timestamp = DateTime.UtcNow
                };

                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    CorrelationId = command.CorrelationId,
                    Type = nameof(RegistrationCancelledEvent),
                    Destination = SagaQueues.RegistrationCancelled,
                    Payload = JsonSerializer.Serialize(cancelledEvent),
                    CreatedAt = DateTime.UtcNow,
                    IsPublished = false
                };

                await _outboxRepository.AddAsync(outboxMessage, cancellationToken);

                _logger.LogInformation(
                    "[CorrelationId={CorrelationId}] RegistrationCancelled published to outbox for UserId={UserId}, EventId={EventId}",
                    command.CorrelationId, command.UserId, command.EventId);
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception($"Greška prilikom otkazivanja registracije: {ex.Message}", ex);
            }
        }
    }
}
