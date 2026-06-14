namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public record UpdateUserEmailCommand(
        Guid UserId,
        string NewEmail,
        Guid CorrelationId);
}
