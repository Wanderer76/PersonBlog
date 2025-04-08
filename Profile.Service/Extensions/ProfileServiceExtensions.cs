using Authentication.Domain.Interfaces;
using Blog.Domain.Services;
using Blog.Service.Services;
using Blog.Service.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Blog.Service.Extensions
{
    public static class ProfileServiceExtensions
    {
        public static void AddProfileServices(this IServiceCollection services)
        {
            services.AddScoped<IPostService, DefaultPostService>();
            services.AddScoped<IBlogService, DefaultBlogService>();
            services.AddScoped<IVideoService, DefaultVideoService>();
            services.AddScoped<IUserPostService, DefaultUserPostService>();
            services.AddScoped<ISubscriptionService, DefaultSubscriptionService>();
            services.AddScoped<ISubscriptionLevelService, DefaultSubscriptionLevelService>();
        }
    }
}
