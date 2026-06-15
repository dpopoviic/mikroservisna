using EventPlatformAPI.UsersAPI.Application.Interfaces;

namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public class DeactivateUserCommandHandler(IUserWriteRepository repository) : ICommandHandler<DeactivateUserCommand>
    {
        public async Task HandleAsync(
            DeactivateUserCommand command,
            CancellationToken cancellationToken = default)
        {
            var aggregate = await repository.LoadAsync(command.UserId, cancellationToken);
            if (aggregate is null)
                throw new Exception("Ne postoji traženi korisnik.");

            try
            {
                aggregate.Deactivate(command.CorrelationId);
                await repository.SaveAsync(aggregate, cancellationToken);

            }
            catch (InvalidOperationException ex)
            {
                throw new Exception($"Greška prilikom deaktiviranja korisnika: {ex.Message}", ex);
            }
        }
    }
}
