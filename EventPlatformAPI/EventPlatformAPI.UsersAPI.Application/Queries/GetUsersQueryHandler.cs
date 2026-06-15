using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.Requests;

namespace EventPlatformAPI.UsersAPI.Application.Queries
{
    public class GetUsersQueryHandler(IUserReadRepository repository) : IQueryHandler<GetUsersQuery, List<UserRequest>>
    {
        public async Task<List<UserRequest>?> HandleAsync(
            GetUsersQuery query,
            CancellationToken cancellationToken = default)
        {
            return await repository.LoadAllAsync(cancellationToken);
        }
    }
}
