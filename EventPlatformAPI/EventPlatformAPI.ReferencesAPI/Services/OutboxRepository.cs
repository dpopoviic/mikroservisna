using EventPlatformAPI.ReferencesAPI.Data;
using EventPlatformAPI.ReferencesAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.ReferencesAPI.Services
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly ReferenceDbContext _db;

        public OutboxRepository(ReferenceDbContext db)
        {
            _db = db;
        }

        public async Task AddOutboxMessageAsync(OutboxMessage message, CancellationToken ct = default)
        {
            _db.OutboxMessages.Add(message);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<List<OutboxMessage>> GetUnpublishedAsync(int maxCount = 10, CancellationToken ct = default)
        {
            return await _db.OutboxMessages
                .Where(x => !x.IsPublished)
                .OrderBy(x => x.CreatedAt)
                .Take(maxCount)
                .ToListAsync(ct);
        }

        public async Task MarkPublishedAsync(IEnumerable<long> ids, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var items = await _db.OutboxMessages.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
            foreach (var it in items)
            {
                it.IsPublished = true;
                it.PublishedAt = now;
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
