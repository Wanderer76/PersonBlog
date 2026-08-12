using Recommendation.Domain.Entities;

namespace Recommendation.Services.Abstractions;

public interface IRecommendationEventStore
{
    Task ExecuteOnceAsync(
        InboxMessage message,
        Func<IRecommendationEventSession, CancellationToken, Task> apply,
        CancellationToken cancellationToken = default);
}

public interface IRecommendationEventSession
{
    Task<PostSnapshot?> GetPostSnapshotAsync(Guid postId, CancellationToken cancellationToken);
    void AddPostSnapshot(PostSnapshot snapshot);
    void AddInteraction(UserInteraction interaction);

    Task<UserCategoryAffinity?> GetCategoryAffinityAsync(
        Guid userId,
        int categoryId,
        CancellationToken cancellationToken);

    void AddCategoryAffinity(UserCategoryAffinity affinity);

    Task<UserBlogAffinity?> GetBlogAffinityAsync(
        Guid userId,
        Guid blogId,
        CancellationToken cancellationToken);

    void AddBlogAffinity(UserBlogAffinity affinity);

    Task<UserSubscription?> GetSubscriptionAsync(
        Guid userId,
        Guid blogId,
        CancellationToken cancellationToken);

    void AddSubscription(UserSubscription subscription);
    void RemoveSubscription(UserSubscription subscription);
}
