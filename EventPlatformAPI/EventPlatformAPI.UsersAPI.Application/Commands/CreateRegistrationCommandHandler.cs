using EventPlatformAPI.UsersAPI.Application.Interfaces;

namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public class CreateRegistrationCommandHandler(IUserWriteRepository repository) : ICommandHandler<CreateRegistrationCommand>
    {
        public async Task HandleAsync(
            CreateRegistrationCommand command,
            CancellationToken cancellationToken = default)
        {
            var aggregate = await repository.LoadAsync(command.UserId, cancellationToken);
            if (aggregate is null)
                throw new Exception("Ne postoji traženi korisnik.");

            try
            {
                aggregate.CreateRegistration(command.EventId, command.CorrelationId);
                await repository.SaveAsync(aggregate, cancellationToken);

            }
            catch (InvalidOperationException ex)
            {
                throw new Exception($"Greška prilikom kreiranja registracije: {ex.Message}", ex);
            }
        }
    }
}
