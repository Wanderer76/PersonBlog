using Blog.Contracts.Services;
using Blog.Service.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Blog.Service.Extensions
{
    public static class ProfileServiceExtensions
    {
        public static void AddBlogServices(this IServiceCollection services)
        {
            services.AddScoped<IPostService, DefaultPostService>();
            services.AddScoped<IProfilePostV2Service, DefaultProfilePostV2Service>();
            services.AddScoped<IBlogService, DefaultBlogService>();
            services.AddScoped<IVideoService, DefaultVideoService>();
            services.AddScoped<IUserPostService, DefaultUserPostService>();
            //services.AddScoped<ISubscriptionService, DefaultSubscriptionService>();
            services.AddScoped<ISubscriptionLevelService, DefaultSubscriptionLevelService>();
            services.AddScoped<ICategoryService, DefaultCategoryService>();
        }
    }
}
