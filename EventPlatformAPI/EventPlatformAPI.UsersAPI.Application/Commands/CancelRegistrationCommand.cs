namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public record CancelRegistrationCommand(
      Guid UserId,
      Guid EventId,
      Guid CorrelationId);
}
