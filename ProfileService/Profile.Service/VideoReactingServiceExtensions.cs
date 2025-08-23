using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Profile.Domain.Services;
using Profile.Service.HttpClients;
using Profile.Service.Implementation;

namespace Profile.Service
{
    public static class VideoReactingServiceExtensions
    {
        public static void AddVideoReactingService(this IServiceCollection services)
        {
            services.AddScoped<IViewHistoryService, DefaultViewHistoryService>();
            services.AddScoped<IReactionService, DefaultReactionService>();
            services.AddScoped<ISubscribeService, DefaultSubscriptionService>();
            services.AddScoped<IProfileService, DefaultProfileService>();
            services.AddScoped<IBanService, DefaultPostBanService>();
        }
        public static void AddProfileHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<ProfileHttpClient>(x =>
            {
                x.BaseAddress = new Uri(configuration["AppUrls:Reacting"]);
            });
        }
    }
}
