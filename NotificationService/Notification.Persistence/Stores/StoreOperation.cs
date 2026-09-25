using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Utils;

namespace Notification.Persistence.Stores;

internal sealed class StoreOperation(DbContextOptions<NotificationDbContext> options,
    ILogger<StoreOperation> logger)
{
    public async Task<Result<T>> Run<T>(Func<NotificationDbContext, Task<Result<T>>> action,
        bool transaction, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new NotificationDbContext(options);
        try
        {
            await using var tx = transaction
                ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : null;
            var result = await action(context);
            if (result.IsSuccess && tx is not null)
            {
                await context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            return result;
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException or TimeoutException)
        {
            logger.LogWarning(exception, "Notification storage operation failed");
            return Result<T>.Failure(new Error("Storage.Unavailable", "Storage is unavailable or the operation conflicted; retry later."));
        }
    }

    public async Task<Result> Run(Func<NotificationDbContext, Task<Result>> action,
        CancellationToken cancellationToken)
    {
        var result = await Run<bool>(async context =>
        {
            var outcome = await action(context);
            return outcome.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(outcome.Errors);
        }, true, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Errors);
    }
}
