namespace Recommendation.Domain.Entities;

public sealed class PostCategory : IRecommendationEntity
{
    public Guid PostId { get; private set; }
    public int CategoryId { get; private set; }
    public PostSnapshot Post { get; private set; } = null!;

    private PostCategory()
    {
    }

    internal PostCategory(Guid postId, int categoryId)
    {
        PostId = postId;
        CategoryId = categoryId;
    }
}
