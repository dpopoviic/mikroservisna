using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.ReadModels;

namespace EventPlatformAPI.UsersAPI.Application.Queries
{
    public class GetUserHistoryQueryHandler(IUserReadRepository repository) :IQueryHandler<GetUserHistoryQuery, List<EventHistoryReadModel>>
    {
        public async Task<List<EventHistoryReadModel>?> HandleAsync(
            GetUserHistoryQuery query,
            CancellationToken cancellationToken = default)
        {
            return await repository.LoadAllHistoryAsync(query.UserId, cancellationToken);
        }
    }

}
