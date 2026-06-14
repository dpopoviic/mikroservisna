using EventPlatformAPI.UsersAPI.Domains.Aggregates;

namespace EventPlatformAPI.UsersAPI.Application.Interfaces
{
    public interface IUserWriteRepository
    {
        Task SaveAsync(UserAggregate user, CancellationToken cancellationToken = default);
        Task<UserAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    }
}
