using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.ReadModels;
using EventPlatformAPI.UsersAPI.Infrastructure.Data;
using EventPlatformAPI.UsersAPI.Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.UsersAPI.Infrastructure.Repositories
{
    public class RegistrationReadRepository(UsersDbContext db) : IRegistrationReadRepository
    {
        public async Task<List<RegistrationRequest>?> LoadAllByEventAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            return await db.Registrations
                           .AsNoTracking()
                           .Where(r => r.EventId == eventId)
                           .OrderByDescending(r => r.CreatedAt)
                           .Select(r => new RegistrationRequest(
                               r.Id,
                               r.UserId,
                               r.EventId,
                               r.Status,
                               r.CreatedAt,
                               r.UserFirstName,
                               r.UserLastName,
                               r.UserEmail))
                           .ToListAsync(cancellationToken);
        }

        public async Task<List<RegistrationRequest>?> LoadAllByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await db.Registrations
                           .AsNoTracking()
                           .Where(r => r.UserId == userId)
                           .OrderByDescending(r => r.CreatedAt)
                           .Select(r => new RegistrationRequest(
                               r.Id,
                               r.UserId,
                               r.EventId,
                               r.Status,
                               r.CreatedAt,
                               r.UserFirstName,
                               r.UserLastName,
                               r.UserEmail))
                           .ToListAsync(cancellationToken);
        }

        public async Task<RegistrationRequest?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var row = await db.Registrations
                           .AsNoTracking()
                           .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (row is null) return null;

            return MapToReadModel(row);
        }

        private static RegistrationRequest MapToReadModel(
        ReadModels.RegistrationReadModel row)
        {
            return new RegistrationRequest(
                row.Id,
                row.UserId,
                row.EventId,
                row.Status,
                row.CreatedAt,
                row.UserFirstName,
                row.UserLastName,
                row.UserEmail);
        }
    }
}
