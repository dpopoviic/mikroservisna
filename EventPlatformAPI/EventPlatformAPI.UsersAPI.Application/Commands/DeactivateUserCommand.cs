namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public record DeactivateUserCommand(
      Guid UserId,
      Guid CorrelationId);
}
