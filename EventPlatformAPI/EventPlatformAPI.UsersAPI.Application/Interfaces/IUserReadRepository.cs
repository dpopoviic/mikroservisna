using EventPlatformAPI.UsersAPI.Application.ReadModels;
using EventPlatformAPI.UsersAPI.Domains.Aggregates;

namespace EventPlatformAPI.UsersAPI.Application.Interfaces
{
    public interface IUserReadRepository
    {

        Task<UserReadModel?> LoadAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<EventHistoryReadModel>?> LoadAllHistoryAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<UserReadModel>?> LoadAllAsync(CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    }
}
