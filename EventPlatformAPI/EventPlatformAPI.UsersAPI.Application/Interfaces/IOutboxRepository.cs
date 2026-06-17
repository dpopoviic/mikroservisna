namespace EventPlatformAPI.UsersAPI.Application.Interfaces;

public interface IOutboxRepository
{
    /// <summary>
    /// Adds an outbox message to the database.
    /// The caller is responsible for ensuring the message will be saved.
    /// </summary>
    Task AddAsync(Domains.Outbox.OutboxMessage message, CancellationToken ct);
}
