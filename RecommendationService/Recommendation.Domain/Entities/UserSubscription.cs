namespace Recommendation.Domain.Entities;

public sealed class UserSubscription : IRecommendationEntity
{
    public Guid UserId { get; private set; }
    public Guid BlogId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private UserSubscription()
    {
    }

    public UserSubscription(Guid userId, Guid blogId, DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        if (blogId == Guid.Empty) throw new ArgumentException("BlogId is required.", nameof(blogId));
        UserId = userId;
        BlogId = blogId;
        CreatedAt = createdAt;
    }
}
