namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public record ConfirmRegistrationCommand(
        Guid UserId,
        int EventId,
        Guid CorrelationId);

}
