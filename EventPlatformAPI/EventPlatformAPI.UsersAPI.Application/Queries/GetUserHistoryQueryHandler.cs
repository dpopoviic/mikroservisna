using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.ReadModels;

namespace EventPlatformAPI.UsersAPI.Application.Queries
{
    public class GetUserHistoryQueryHandler(IUserReadRepository repository) :IQueryHandler<GetUserHistoryQuery, List<EventHistoryRequest>>
    {
        public async Task<List<EventHistoryRequest>?> HandleAsync(
            GetUserHistoryQuery query,
            CancellationToken cancellationToken = default)
        {
            return await repository.LoadAllHistoryAsync(query.UserId, cancellationToken);
        }
    }

}
