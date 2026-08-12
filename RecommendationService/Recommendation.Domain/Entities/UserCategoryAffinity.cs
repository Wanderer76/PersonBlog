namespace Recommendation.Domain.Entities;

public sealed class UserCategoryAffinity : IRecommendationEntity
{
    public Guid UserId { get; private set; }
    public int CategoryId { get; private set; }
    public double Score { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private UserCategoryAffinity()
    {
    }

    public UserCategoryAffinity(Guid userId, int categoryId, double score, DateTimeOffset updatedAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        if (categoryId <= 0) throw new ArgumentOutOfRangeException(nameof(categoryId));
        UserId = userId;
        CategoryId = categoryId;
        Update(score, updatedAt);
    }

    public void Update(double score, DateTimeOffset updatedAt)
    {
        if (!double.IsFinite(score)) throw new ArgumentOutOfRangeException(nameof(score));
        Score = score;
        UpdatedAt = updatedAt;
    }
}
