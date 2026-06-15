using EventPlatformAPI.SagaOrcgestrator.Entities;

namespace EventPlatformAPI.SagaOrcgestrator.Interfaces
{
    public interface IRegistrationSagaStateRepository
    {
        Task<RegistrationSagaState?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken ct = default);
        Task AddAsync(RegistrationSagaState state, CancellationToken ct = default);
        Task UpdateAsync(RegistrationSagaState state, CancellationToken ct = default);
    }
}
