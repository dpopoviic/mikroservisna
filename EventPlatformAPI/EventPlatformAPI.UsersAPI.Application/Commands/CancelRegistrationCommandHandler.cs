using EventPlatformAPI.UsersAPI.Application.Interfaces;

namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public class CancelRegistrationCommandHandler(IUserWriteRepository repository) : ICommandHandler<CancelRegistrationCommand>
    {
        public async Task HandleAsync(
            CancelRegistrationCommand command,
            CancellationToken cancellationToken = default)
        {
            var aggregate = await repository.LoadAsync(command.UserId, cancellationToken);
            if (aggregate is null)
                throw new Exception("Ne postoji traženi korisnik.");

            try
            {
                aggregate.CancelRegistration(command.EventId, command.CorrelationId);
                await repository.SaveAsync(aggregate, cancellationToken);

            }
            catch (InvalidOperationException ex)
            {
                throw new Exception($"Greška prilikom otkazivanja registracije: {ex.Message}", ex);
            }

        }
    }
}
