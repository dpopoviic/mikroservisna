using EventPlatformAPI.UsersAPI.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EventPlatformAPI.UsersAPI.Application.Providers
{

    public class QueryDispatcher(IServiceProvider provider) : IQueryDispatcher
    {
        public Task<TResult> Dispatch<TQuery, TResult>(TQuery query, CancellationToken ct)
        {
            var handler = provider.GetRequiredService<IQueryHandler<TQuery, TResult>>();

            return handler.HandleAsync(query, ct);
        }
    }
}
