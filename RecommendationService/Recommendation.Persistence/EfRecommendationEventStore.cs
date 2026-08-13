using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Recommendation.Domain.Entities;
using Recommendation.Services.Abstractions;

namespace Recommendation.Persistence;

public sealed class EfRecommendationEventStore(
    RecommendationDbContext dbContext,
    IClock clock) : IRecommendationEventStore, IRecommendationEventSession
{
    public async Task ExecuteOnceAsync(
        InboxMessage message,
        Func<IRecommendationEventSession, CancellationToken, Task> apply,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(apply);

        IDbContextTransaction? transaction = null;
        try
        {
            if (dbContext.Database.IsRelational())
            {
                transaction = await dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);
            }

            var alreadyProcessed = await dbContext.InboxMessages
                .AsNoTracking()
                .AnyAsync(x => x.EventId == message.EventId, cancellationToken);
            if (alreadyProcessed)
            {
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                return;
            }

            // Persist the inbox key before applying the projection. Within the same
            // transaction this reserves EventId and makes concurrent duplicates fail.
            dbContext.InboxMessages.Add(message);
            await dbContext.SaveChangesAsync(cancellationToken);

            await apply(this, cancellationToken);
            message.MarkProcessed(clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            else
            {
                await RemoveInboxReservationAsync(message.EventId);
            }

            dbContext.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public Task<PostSnapshot?> GetPostSnapshotAsync(
        Guid postId,
        CancellationToken cancellationToken) =>
        dbContext.PostSnapshots
            .Include(x => x.Categories)
            .SingleOrDefaultAsync(x => x.PostId == postId, cancellationToken);

    public void AddPostSnapshot(PostSnapshot snapshot) => dbContext.PostSnapshots.Add(snapshot);

    public void AddInteraction(UserInteraction interaction) => dbContext.UserInteractions.Add(interaction);

    public Task<UserCategoryAffinity?> GetCategoryAffinityAsync(
        Guid userId,
        int categoryId,
        CancellationToken cancellationToken) =>
        dbContext.UserCategoryAffinities.SingleOrDefaultAsync(
            x => x.UserId == userId && x.CategoryId == categoryId,
            cancellationToken);

    public void AddCategoryAffinity(UserCategoryAffinity affinity) =>
        dbContext.UserCategoryAffinities.Add(affinity);

    public Task<UserBlogAffinity?> GetBlogAffinityAsync(
        Guid userId,
        Guid blogId,
        CancellationToken cancellationToken) =>
        dbContext.UserBlogAffinities.SingleOrDefaultAsync(
            x => x.UserId == userId && x.BlogId == blogId,
            cancellationToken);

    public void AddBlogAffinity(UserBlogAffinity affinity) =>
        dbContext.UserBlogAffinities.Add(affinity);

    public Task<UserSubscription?> GetSubscriptionAsync(
        Guid userId,
        Guid blogId,
        CancellationToken cancellationToken) =>
        dbContext.UserSubscriptions.SingleOrDefaultAsync(
            x => x.UserId == userId && x.BlogId == blogId,
            cancellationToken);

    public void AddSubscription(UserSubscription subscription) =>
        dbContext.UserSubscriptions.Add(subscription);

    public void RemoveSubscription(UserSubscription subscription) =>
        dbContext.UserSubscriptions.Remove(subscription);

    private async Task RemoveInboxReservationAsync(Guid eventId)
    {
        // Only used by non-relational test providers, which cannot roll back a transaction.
        dbContext.ChangeTracker.Clear();
        var reservation = await dbContext.InboxMessages.FindAsync([eventId], CancellationToken.None);
        if (reservation is null) return;
        dbContext.InboxMessages.Remove(reservation);
        await dbContext.SaveChangesAsync(CancellationToken.None);
    }
}
