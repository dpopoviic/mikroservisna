namespace EventPlatformAPI.UsersAPI.Application.Commands
{
    public record CancelRegistrationCommand(
      Guid UserId,
      int EventId,
      Guid CorrelationId);
}
