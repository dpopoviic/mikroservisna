namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public record ActivateUserCommand(
        Guid UserId,
        Guid CorrelationId);

}
