namespace EventPlatformAPI.UsersAPI.Infrastructure.Messaging
{
    public interface IOutboxPublisher
    {
        Task PublishAsync(string destination, Guid messageId, string payload, string type, CancellationToken ct);
    }
}
