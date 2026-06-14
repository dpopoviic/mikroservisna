using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.ReadModels;

namespace EventPlatformAPI.UsersAPI.Application.Queries
{
    public class GetUserByIdQueryHandler(IUserReadRepository repository) : IQueryHandler<GetUserByIdQuery, UserReadModel>
    {

        public async Task<UserReadModel?> HandleAsync(
            GetUserByIdQuery query,
            CancellationToken cancellationToken = default)
        {
            return await repository.LoadAsync(query.UserId, cancellationToken);
        }
    }
}