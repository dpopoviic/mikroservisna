using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.ReadModels;

namespace EventPlatformAPI.UsersAPI.Application.Queries
{
    public class GetRegistrationsForEventQueryHandler(IRegistrationReadRepository repository) : IQueryHandler<GetRegistrationsForEventQuery, List<RegistrationRequest>?>
    {
        public Task<List<RegistrationRequest>?> HandleAsync(GetRegistrationsForEventQuery query, CancellationToken ct)
        {
            return repository.LoadAllByEventAsync(query.EventId, ct);
        }
    }
}
