using EventPlatformAPI.UsersAPI.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public class CancelRegistrationCommandHandler : ICommandHandler<CancelRegistrationCommand>
    {
        private readonly IUserWriteRepository _repository;
        private readonly ILogger<CancelRegistrationCommandHandler> _logger;

        public CancelRegistrationCommandHandler(
            IUserWriteRepository repository,
            ILogger<CancelRegistrationCommandHandler> logger)
        {
            _repository = repository;
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
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception($"Greška prilikom otkazivanja registracije: {ex.Message}", ex);
            }
        }
    }
}
