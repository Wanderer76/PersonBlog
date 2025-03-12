using Blog.Service.Models;

namespace Recommendation.Service.Service
{
    public interface IRecommendationService
    {
        Task<IEnumerable<VideoCardModel>> GetRecommendationsAsync(int page, int pageSize);
        Task<IEnumerable<VideoCardModel>> GetRecommendationsAsync(int page, int pageSize, Guid? currentPostId);
    }
}
