using EventPlatformAPI.UsersAPI.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public class CreateRegistrationCommandHandler : ICommandHandler<CreateRegistrationCommand>
    {
        private readonly IUserWriteRepository _repository;
        private readonly ILogger<CreateRegistrationCommandHandler> _logger;

        public CreateRegistrationCommandHandler(
            IUserWriteRepository repository,
            ILogger<CreateRegistrationCommandHandler> logger)
        {
            _repository = repository;
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
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception($"Greška prilikom kreiranja registracije: {ex.Message}", ex);
            }
        }
    }
}
