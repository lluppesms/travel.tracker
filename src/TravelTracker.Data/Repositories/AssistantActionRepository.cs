using System.Data;

using Microsoft.EntityFrameworkCore.Storage;

namespace TravelTracker.Data.Repositories;

public sealed class AssistantActionRepository(TravelTrackerDbContext context) : IAssistantActionRepository
{
    private readonly TravelTrackerDbContext _context = context;

    public Task<AssistantAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.AssistantActions
            .AsNoTracking()
            .SingleOrDefaultAsync(action => action.Id == id, cancellationToken);

    public Task<AssistantAction?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.AssistantActions
            .FromSqlInterpolated($"""
                SELECT *
                FROM [Travel].[AssistantActions] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {id}
                """)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<AssistantAction?> GetByIdempotencyKeyAsync(
        int userId,
        string threadId,
        string canonicalIdempotencyKey,
        CancellationToken cancellationToken = default) =>
        _context.AssistantActions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                action => action.UserId == userId
                    && action.ThreadId == threadId
                    && action.CanonicalIdempotencyKey == canonicalIdempotencyKey,
                cancellationToken);

    public async Task<IReadOnlyList<AssistantAction>> GetPendingAsync(
        int userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        await _context.AssistantActions
            .AsNoTracking()
            .Where(action => action.UserId == userId
                && action.State == AssistantActionStates.Pending
                && action.ExpiresAt > utcNow)
            .OrderByDescending(action => action.CreatedDate)
            .ToListAsync(cancellationToken);

    public Task AddAsync(AssistantAction action, CancellationToken cancellationToken = default) =>
        _context.AssistantActions.AddAsync(action, cancellationToken).AsTask();

    public void Detach(AssistantAction action) =>
        _context.Entry(action).State = EntityState.Detached;

    public void ClearTracking() =>
        _context.ChangeTracker.Clear();

    public Task<IDbContextTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken = default) =>
        _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<int> ExpirePendingAsync(
        DateTime utcNow,
        DateTime retainUntil,
        CancellationToken cancellationToken = default) =>
        await _context.AssistantActions
            .Where(action => action.State == AssistantActionStates.Pending && action.ExpiresAt <= utcNow)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(action => action.State, AssistantActionStates.Expired)
                    .SetProperty(action => action.CanonicalCommandCiphertext, (string?)null)
                    .SetProperty(action => action.ErrorCode, "action_expired")
                    .SetProperty(action => action.CompletedDate, utcNow)
                    .SetProperty(action => action.ModifiedDate, utcNow)
                    .SetProperty(action => action.RetainUntilDate, retainUntil),
                cancellationToken);

    public async Task<int> DeleteRetainedAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
        await _context.AssistantActions
            .Where(action => action.State != AssistantActionStates.Pending
                && action.State != AssistantActionStates.Executing
                && action.RetainUntilDate <= utcNow)
            .ExecuteDeleteAsync(cancellationToken);
}

public static class AssistantActionStates
{
    public const string Pending = "Pending";
    public const string Executing = "Executing";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
    public const string Failed = "Failed";
}
