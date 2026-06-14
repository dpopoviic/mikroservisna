using EventPlatformAPI.UsersAPI.Application.ReadModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatformAPI.UsersAPI.Application.Interfaces
{
    public interface IRegistrationReadRepository
    {
        Task<RegistrationReadModel> LoadAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<RegistrationReadModel>?> LoadAllByUserAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<RegistrationReadModel>?> LoadAllByEventAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
