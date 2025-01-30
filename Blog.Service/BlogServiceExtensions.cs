using Blog.Service.Service;
using Blog.Service.Service.Implementaion;
using Microsoft.Extensions.DependencyInjection;

namespace Blog.Service
{
    public static class BlogServiceExtensions
    {
        public static void AddBlogServices(this IServiceCollection service)
        {
            service.AddScoped<IRecommendationService,DefaultRecommendationService>();
        }
    }
}
