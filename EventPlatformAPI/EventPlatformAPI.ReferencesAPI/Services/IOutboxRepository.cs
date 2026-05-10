using EventPlatformAPI.ReferencesAPI.Models;

namespace EventPlatformAPI.ReferencesAPI.Services
{
    public interface IOutboxRepository
    {
        Task AddOutboxMessageAsync(OutboxMessage message, CancellationToken ct = default);
        Task<List<OutboxMessage>> GetUnpublishedAsync(int maxCount = 10, CancellationToken ct = default);
        Task MarkPublishedAsync(IEnumerable<long> ids, CancellationToken ct = default);
    }
}
