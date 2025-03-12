using Microsoft.Extensions.DependencyInjection;
using Recommendation.Service.Service;
using Recommendation.Service.Service.Implementaion;

namespace Recommendation.Service
{
    public static class RecommendationServiceExtensions
    {
        public static void AddBlogServices(this IServiceCollection service)
        {
            service.AddScoped<IRecommendationService, DefaultRecommendationService>();
        }
    }
}
