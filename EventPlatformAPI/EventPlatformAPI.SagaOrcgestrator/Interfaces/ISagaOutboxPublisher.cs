namespace EventPlatformAPI.SagaOrcgestrator.Interfaces
{
    public interface ISagaOutboxPublisher
    {
        Task PublishAsync(string destination, Guid messageId, string payload,
            string messageType, CancellationToken ct = default);
    }
}
