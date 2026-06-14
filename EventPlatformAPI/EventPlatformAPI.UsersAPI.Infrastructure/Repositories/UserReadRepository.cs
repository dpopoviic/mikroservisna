using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.ReadModels;
using EventPlatformAPI.UsersAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.UsersAPI.Infrastructure.Repositories
{
    public class UserReadRepository(UsersDbContext db) : IUserReadRepository
    {
        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            return await db.Users
                           .AsNoTracking()
                           .AnyAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<List<UserRequest>?> LoadAllAsync(CancellationToken cancellationToken = default)
        {
            return await db.Users
                           .AsNoTracking()
                           .OrderBy(u => u.LastName)
                           .ThenBy(u => u.FirstName)
                           .Select(u => new UserRequest(
                               u.Id,
                               u.FirstName,
                               u.LastName,
                               u.Email,
                               u.IsActive,
                               u.RegistrationsCount))
                           .ToListAsync(cancellationToken);
        }

        public async Task<List<EventHistoryRequest>?> LoadAllHistoryAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await db.EventStore
                            .AsNoTracking()
                            .Where(e => e.AggregateId == id)
                            .OrderBy(e => e.Version)
                            .Select(e => new EventHistoryRequest(
                                e.EventType,
                                e.Version,
                                e.CorrelationId,
                                e.OccurredOn))
                            .ToListAsync(cancellationToken);
        }

        public async Task<UserRequest?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var row = await db.Users
                           .AsNoTracking()
                           .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (row is null) return null;

            return new UserRequest(
                row.Id,
                row.FirstName,
                row.LastName,
                row.Email,
                row.IsActive,
                row.RegistrationsCount);
        }

    }
}
