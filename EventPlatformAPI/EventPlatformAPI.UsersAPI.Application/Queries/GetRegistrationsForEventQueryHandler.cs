using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.ReadModels;

namespace EventPlatformAPI.UsersAPI.Application.Queries
{
    public class GetRegistrationsForEventQueryHandler(IRegistrationReadRepository repository) : IQueryHandler<GetRegistrationsForEventQuery, List<RegistrationReadModel>>
    {
        public Task<List<RegistrationReadModel>?> HandleAsync(GetRegistrationsForEventQuery query, CancellationToken ct)
        {
            return repository.LoadAllByEventAsync(query.EventId, ct);
        }
    }
}
