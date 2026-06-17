using EventPlatformAPI.SagaOrcgestrator.Data;
using EventPlatformAPI.SagaOrcgestrator.Entities;
using EventPlatformAPI.SagaOrcgestrator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.SagaOrcgestrator.Repositories
{
    public class RegistrationSagaStateRepository(SagaDbContext db) : IRegistrationSagaStateRepository
    {
        public async Task<RegistrationSagaState?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken ct = default)
            => await db.RegistrationSagaStates
                 .FirstOrDefaultAsync(x => x.CorrelationId == correlationId, ct);

        public async Task AddAsync(RegistrationSagaState state, CancellationToken ct = default)
        {
            db.RegistrationSagaStates.Add(state);
            await db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(RegistrationSagaState state, CancellationToken ct = default)
        {
            db.RegistrationSagaStates.Update(state);
            await db.SaveChangesAsync(ct);
        }
    }
}
