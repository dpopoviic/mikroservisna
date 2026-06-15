using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Domains.Aggregates;

namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public class CreateUserCommandHandler(IUserWriteRepository repository) : ICommandHandler<CreateUserCommand>
    {

        public async Task HandleAsync(
            CreateUserCommand command,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.FirstName))
                throw new Exception("Ime je obavezno.");

            if (string.IsNullOrWhiteSpace(command.LastName))
                throw new Exception("Prezime je obavezno.");
                

            if (string.IsNullOrWhiteSpace(command.Email))
                throw new Exception("Email je obavezan.");

            if (await repository.EmailExistsAsync(command.Email, cancellationToken))
                throw new Exception($"Email '{command.Email}' je već u upotrebi.");

            var aggregate = UserAggregate.Create(
                command.UserId,
                command.FirstName,
                command.LastName,
                command.Email,
                command.CorrelationId);
            try
            {
                await repository.SaveAsync(aggregate, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Greška prilikom kreiranja korisnika: {ex.Message}", ex);
            }

        }
    }
}
