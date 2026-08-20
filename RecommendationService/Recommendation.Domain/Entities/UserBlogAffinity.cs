using Shared.Utils;

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

    private UserBlogAffinity(Guid userId, Guid blogId, double score, DateTimeOffset updatedAt)
    {
        UserId = userId;
        BlogId = blogId;
        Score = score;
        UpdatedAt = updatedAt;
    }

    public static Result<UserBlogAffinity> Create(
        Guid userId,
        Guid blogId,
        double score,
        DateTimeOffset updatedAt)
    {
        if (userId == Guid.Empty)
            return new Error(nameof(userId), "UserId is required.");
        if (blogId == Guid.Empty)
            return new Error(nameof(blogId), "BlogId is required.");
        if (!double.IsFinite(score))
            return new Error(nameof(score), "Score must be finite.");

        return new UserBlogAffinity(userId, blogId, score, updatedAt);
    }

    public void Update(double score, DateTimeOffset updatedAt)
    {
        if (!double.IsFinite(score)) throw new ArgumentOutOfRangeException(nameof(score));
        Score = score;
        UpdatedAt = updatedAt;
    }
}
