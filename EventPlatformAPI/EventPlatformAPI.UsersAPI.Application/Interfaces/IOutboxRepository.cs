namespace EventPlatformAPI.UsersAPI.Application.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(Domains.Outbox.OutboxMessage message, CancellationToken ct);
}
