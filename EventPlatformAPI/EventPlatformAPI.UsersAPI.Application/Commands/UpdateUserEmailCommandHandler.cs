using EventPlatformAPI.UsersAPI.Application.Interfaces;

namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public class UpdateUserEmailCommandHandler(IUserWriteRepository repository) : ICommandHandler<UpdateUserEmailCommand>
    {
        public async Task HandleAsync(
            UpdateUserEmailCommand command,
            CancellationToken cancellationToken = default)
        {
            var aggregate = await repository.LoadAsync(command.UserId, cancellationToken);
            if (aggregate is null)
                throw new Exception("Ne postoji traženi korisnik.");

            if (await repository.EmailExistsAsync(command.NewEmail, cancellationToken))
                throw new Exception($"Email {command.NewEmail} se vec koristi.");

            try
            {
                aggregate.ChangeEmail(command.NewEmail, command.CorrelationId);
                await repository.SaveAsync(aggregate, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception($"Greška prilikom promene email-a korisnika: {ex.Message}", ex);
            }
        }
    }
}
