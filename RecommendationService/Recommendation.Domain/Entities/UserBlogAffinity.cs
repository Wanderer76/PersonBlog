namespace Recommendation.Domain.Entities;

public sealed class UserBlogAffinity : IRecommendationEntity
{
    public Guid UserId { get; private set; }
    public Guid BlogId { get; private set; }
    public double Score { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private UserBlogAffinity()
    {
    }

    public UserBlogAffinity(Guid userId, Guid blogId, double score, DateTimeOffset updatedAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        if (blogId == Guid.Empty) throw new ArgumentException("BlogId is required.", nameof(blogId));
        UserId = userId;
        BlogId = blogId;
        Update(score, updatedAt);
    }

    public void Update(double score, DateTimeOffset updatedAt)
    {
        if (!double.IsFinite(score)) throw new ArgumentOutOfRangeException(nameof(score));
        Score = score;
        UpdatedAt = updatedAt;
    }
}
