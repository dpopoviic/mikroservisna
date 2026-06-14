using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.ReadModels;

namespace EventPlatformAPI.UsersAPI.Application.Queries
{
    public class GetUserRegistrationsQueryHandler(IRegistrationReadRepository repository) : IQueryHandler<GetUserRegistrationsQuery, List<RegistrationReadModel>>
    {
        public async Task<List<RegistrationReadModel>?> HandleAsync(
            GetUserRegistrationsQuery query,
            CancellationToken cancellationToken = default)
        {
            return await repository.LoadAllAsync(query.UserId, cancellationToken);
        }
    }

}
