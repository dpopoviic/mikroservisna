using EventPlatformAPI.UsersAPI.Application.Requests;

namespace EventPlatformAPI.UsersAPI.Application.Interfaces
{
    public interface IRegistrationReadRepository
    {
        Task<RegistrationRequest?> LoadAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<RegistrationRequest>?> LoadAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<RegistrationRequest>?> LoadAllByEventAsync(int eventId, CancellationToken cancellationToken = default);
    }
}
