using Microsoft.EntityFrameworkCore.Storage;

namespace TravelTracker.Data.Repositories;

public interface IAssistantActionRepository
{
    Task<AssistantAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssistantAction?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssistantAction?> GetByIdempotencyKeyAsync(
        int userId,
        string threadId,
        string canonicalIdempotencyKey,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssistantAction>> GetPendingAsync(
        int userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
    Task AddAsync(AssistantAction action, CancellationToken cancellationToken = default);
    void Detach(AssistantAction action);
    void ClearTracking();
    Task<IDbContextTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> ExpirePendingAsync(DateTime utcNow, DateTime retainUntil, CancellationToken cancellationToken = default);
    Task<int> DeleteRetainedAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
