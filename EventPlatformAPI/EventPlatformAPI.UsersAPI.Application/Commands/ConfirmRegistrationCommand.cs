namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public record ConfirmRegistrationCommand(
        Guid UserId,
        Guid EventId,
        Guid CorrelationId);

}
