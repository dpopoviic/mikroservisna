namespace EventPlatformAPI.UsersAPI.Application.Commands
{

    public record CreateRegistrationCommand(
        Guid UserId,
        Guid EventId,
        Guid CorrelationId);
}
