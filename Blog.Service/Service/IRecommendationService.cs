using Blog.Service.Models;

namespace Blog.Service.Service
{
    public interface IRecommendationService
    {
        Task<IEnumerable<VideoCardModel>> GetRecommendations(int page, int pageSize);
    }
}
