using Shared.Utils;

namespace Recommendation.Domain.Entities;

public sealed class UserSubscription : IRecommendationEntity
{
    public Guid UserId { get; private set; }
    public Guid BlogId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private UserSubscription()
    {
    }

    private UserSubscription(Guid userId, Guid blogId, DateTimeOffset createdAt)
    {
        UserId = userId;
        BlogId = blogId;
        CreatedAt = createdAt;
    }

    public static Result<UserSubscription> Create(Guid userId, Guid blogId, DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty)
            return new Error(nameof(userId), "UserId is required.");
        if (blogId == Guid.Empty)
            return new Error(nameof(blogId), "BlogId is required.");

        return new UserSubscription(userId, blogId, createdAt);
    }
}
