using EventPlatformAPI.UsersAPI.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EventPlatformAPI.UsersAPI.Application.Providers
{
    public class CommandDispatcher(IServiceProvider provider) : ICommandDispatcher
    {
        public Task Dispatch<TCommand>(TCommand command, CancellationToken ct)
        {
            var handler = provider.GetRequiredService<ICommandHandler<TCommand>>();

            return handler.HandleAsync(command, ct);
        }
    }
}
