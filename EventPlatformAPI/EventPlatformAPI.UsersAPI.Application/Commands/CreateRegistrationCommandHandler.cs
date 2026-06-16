using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.Messages.Saga;
using EventPlatformAPI.Messages.Saga.UserApiMessages;
using EventPlatformAPI.UsersAPI.Domains.Outbox;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public class CreateRegistrationCommandHandler : ICommandHandler<CreateRegistrationCommand>
    {
        private readonly IUserWriteRepository _repository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly ILogger<CreateRegistrationCommandHandler> _logger;

        public CreateRegistrationCommandHandler(
            IUserWriteRepository repository,
            IOutboxRepository outboxRepository,
            ILogger<CreateRegistrationCommandHandler> logger)
        {
            _repository = repository;
            _outboxRepository = outboxRepository;
            _logger = logger;
        }

        public async Task HandleAsync(
            CreateRegistrationCommand command,
            CancellationToken cancellationToken = default)
        {
            var aggregate = await _repository.LoadAsync(command.UserId, cancellationToken);
            if (aggregate is null)
                throw new Exception("Ne postoji traženi korisnik.");

            try
            {
                aggregate.CreateRegistration(command.EventId, command.CorrelationId);
                await _repository.SaveAsync(aggregate, cancellationToken);

                var registrationEvent = new RegistrationRequestedEvent
                {
                    CorrelationId = command.CorrelationId,
                    RegistrationId = Guid.NewGuid(),
                    UserId = command.UserId,
                    EventId = command.EventId,
                    UserEmail = aggregate.Email,
                    UserFirstName = aggregate.FirstName,
                    UserLastName = aggregate.LastName,
                    Timestamp = DateTime.UtcNow
                };

                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    CorrelationId = command.CorrelationId,
                    Type = nameof(RegistrationRequestedEvent),
                    Destination = SagaQueues.RegistrationRequested,
                    Payload = JsonSerializer.Serialize(registrationEvent),
                    CreatedAt = DateTime.UtcNow,
                    IsPublished = false
                };

                await _outboxRepository.AddAsync(outboxMessage, cancellationToken);

                _logger.LogInformation(
                    "[CorrelationId={CorrelationId}] RegistrationRequested published to outbox for UserId={UserId}, EventId={EventId}",
                    command.CorrelationId, command.UserId, command.EventId);
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception($"Greška prilikom kreiranja registracije: {ex.Message}", ex);
            }
        }
    }
}
