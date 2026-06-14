namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public record CreateUserCommand(
        Guid UserId,
       string FirstName,
       string LastName,
       string Email,
       Guid CorrelationId);
}
