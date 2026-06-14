using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.ReadModels;

namespace EventPlatformAPI.UsersAPI.Application.Queries
{
    public class GetUserRegistrationsQueryHandler(IRegistrationReadRepository repository) : IQueryHandler<GetUserRegistrationsQuery, List<RegistrationRequest>>
    {
        public async Task<List<RegistrationRequest>?> HandleAsync(
            GetUserRegistrationsQuery query,
            CancellationToken cancellationToken = default)
        {
            return await repository.LoadAllByUserAsync(query.UserId, cancellationToken);
        }
    }

}
