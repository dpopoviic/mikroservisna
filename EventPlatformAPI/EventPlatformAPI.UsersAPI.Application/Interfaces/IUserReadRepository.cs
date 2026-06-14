using EventPlatformAPI.UsersAPI.Application.ReadModels;
using EventPlatformAPI.UsersAPI.Domains.Aggregates;

namespace EventPlatformAPI.UsersAPI.Application.Interfaces
{
    public interface IUserReadRepository
    {

        Task<UserRequest?> LoadAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<EventHistoryRequest>?> LoadAllHistoryAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<UserRequest>?> LoadAllAsync(CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    }
}
