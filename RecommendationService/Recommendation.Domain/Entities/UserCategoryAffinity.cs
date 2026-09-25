using Shared.Utils;

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

    private UserCategoryAffinity(Guid userId, int categoryId, double score, DateTimeOffset updatedAt)
    {
        UserId = userId;
        CategoryId = categoryId;
        Score = score;
        UpdatedAt = updatedAt;
    }

    public static Result<UserCategoryAffinity> Create(
        Guid userId,
        int categoryId,
        double score,
        DateTimeOffset updatedAt)
    {
        if (userId == Guid.Empty)
            return new Error(nameof(userId), "UserId is required.");
        if (categoryId <= 0)
            return new Error(nameof(categoryId), "CategoryId must be positive.");
        if (!double.IsFinite(score))
            return new Error(nameof(score), "Score must be finite.");

        return new UserCategoryAffinity(userId, categoryId, score, updatedAt);
    }

    public void Update(double score, DateTimeOffset updatedAt)
    {
        if (!double.IsFinite(score)) throw new ArgumentOutOfRangeException(nameof(score));
        Score = score;
        UpdatedAt = updatedAt;
    }
}
