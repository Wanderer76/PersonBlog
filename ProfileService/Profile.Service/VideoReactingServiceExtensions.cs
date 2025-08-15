using Microsoft.Extensions.DependencyInjection;
using Profile.Domain.Services;
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
        }
    }
}
