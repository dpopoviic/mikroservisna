using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Domains.Outbox;
using EventPlatformAPI.UsersAPI.Infrastructure.Data;

namespace EventPlatformAPI.UsersAPI.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly UsersDbContext _db;

    public OutboxRepository(UsersDbContext db) => _db = db;

    public async Task AddAsync(OutboxMessage message, CancellationToken ct)
    {
        _db.OutboxMessages.Add(message);
        await _db.SaveChangesAsync(ct);
    }
}
