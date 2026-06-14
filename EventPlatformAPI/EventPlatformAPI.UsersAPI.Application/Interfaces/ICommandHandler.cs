using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatformAPI.UsersAPI.Application.Interfaces
{
    public interface ICommandHandler<TCommand>
    {
        Task HandleAsync(TCommand command, CancellationToken ct);
    }
}
