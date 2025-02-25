using Microsoft.Extensions.DependencyInjection;
using Profile.Service.Services;
using Profile.Service.Services.Implementation;

namespace Profile.Service.Extensions
{
    public static class ProfileServiceExtensions
    {
        public static void AddProfileServices(this IServiceCollection services)
        {
            services.AddScoped<IProfileService, DefaultProfileService>();
            services.AddScoped<IPostService, DefaultPostService>();
            services.AddScoped<IBlogService, DefaultBlogService>();
            services.AddScoped<IVideoService, DefaultVideoService>();
            services.AddScoped<IUserPostService, DefaultUserPostService>();
            services.AddScoped<ISubscriptionService, DefaultSubscriptionService>();
        }
    }
}
