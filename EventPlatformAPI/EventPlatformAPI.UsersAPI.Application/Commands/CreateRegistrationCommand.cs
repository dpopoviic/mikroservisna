namespace EventPlatformAPI.UsersAPI.Application.Commands
{

    public record CreateRegistrationCommand(
        Guid UserId,
        int EventId,
        Guid CorrelationId);
}
