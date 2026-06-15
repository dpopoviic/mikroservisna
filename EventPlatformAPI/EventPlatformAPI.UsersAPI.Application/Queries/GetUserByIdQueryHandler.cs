using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.Requests;

namespace EventPlatformAPI.UsersAPI.Application.Queries
{
    public class GetUserByIdQueryHandler(IUserReadRepository repository) : IQueryHandler<GetUserByIdQuery, UserRequest>
    {

        public async Task<UserRequest?> HandleAsync(
            GetUserByIdQuery query,
            CancellationToken cancellationToken = default)
        {
            return await repository.LoadAsync(query.UserId, cancellationToken);
        }
    }
}