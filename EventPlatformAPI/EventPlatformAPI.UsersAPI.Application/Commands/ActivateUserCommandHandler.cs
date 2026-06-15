using EventPlatformAPI.UsersAPI.Application.Interfaces;

namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public class ActivateUserCommandHandler(IUserWriteRepository repository) : ICommandHandler<ActivateUserCommand>
    {
        public async Task HandleAsync(ActivateUserCommand command, CancellationToken cancellationToken = default)
        {
            var aggregate = await repository.LoadAsync(command.UserId, cancellationToken);
            if (aggregate is null)
                throw new Exception("Ne postoji traženi korisnik.");

            try
            {
                aggregate.Activate(command.CorrelationId);
                await repository.SaveAsync(aggregate, cancellationToken);

            }
            catch (InvalidOperationException ex)
            {
                throw new Exception($"Greška prilikom aktiviranja korisnika: {ex.Message}", ex);
            }
        }
    }
}
