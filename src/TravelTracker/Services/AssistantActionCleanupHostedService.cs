using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using TravelTracker.Data.Configuration;
using TravelTracker.Data.Repositories;

namespace TravelTracker.Services;

internal sealed class AssistantActionCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<TravelAssistantOptions> options,
    ILogger<AssistantActionCleanupHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCleanupAsync(stoppingToken);
            await Task.Delay(CleanupInterval, timeProvider, stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAssistantActionRepository>();
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var retainUntil = now.AddDays(Math.Max(1, options.Value.ActionAuditRetentionDays));
            var expired = await repository.ExpirePendingAsync(now, retainUntil, cancellationToken);
            var deleted = await repository.DeleteRetainedAsync(now, cancellationToken);

            if (expired > 0 || deleted > 0)
            {
                logger.LogInformation(
                    "Assistant action cleanup expired {ExpiredCount} pending actions and deleted {DeletedCount} retained audit rows.",
                    expired,
                    deleted);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Assistant action cleanup failed.");
        }
        catch (DbException exception)
        {
            logger.LogError(exception, "Assistant action cleanup failed.");
        }
    }
}
