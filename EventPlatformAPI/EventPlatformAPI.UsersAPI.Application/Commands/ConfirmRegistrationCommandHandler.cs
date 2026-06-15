using EventPlatformAPI.UsersAPI.Application.Interfaces;

namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public class ConfirmRegistrationCommandHandler(IUserWriteRepository repository) : ICommandHandler<ConfirmRegistrationCommand>
    {
        public async Task HandleAsync(
            ConfirmRegistrationCommand command,
            CancellationToken cancellationToken = default)
        {
            var aggregate = await repository.LoadAsync(command.UserId, cancellationToken);
            if (aggregate is null)
                throw new Exception("Ne postoji traženi korisnik.");

            try
            {
                aggregate.ConfirmRegistration(command.EventId, command.CorrelationId);
                await repository.SaveAsync(aggregate, cancellationToken);

            }
            catch (InvalidOperationException ex)
            {
                throw new Exception($"Greška prilikom potvrde registracije: {ex.Message}", ex);

            }
        }
    }
}
