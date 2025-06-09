using Microsoft.Extensions.DependencyInjection;
using VideoReacting.Service.Implementation;
using ViewReacting.Domain.Services;

namespace VideoReacting.Service
{
    public static class VideoReactingServiceExtensions
    {
        public static void AddVideoReactingService(this IServiceCollection services)
        {
            services.AddScoped<IViewHistoryService, DefaultViewHistoryService>();
            services.AddScoped<IReactionService, DefaultReactionService>();
            services.AddScoped<ISubscribeService, DefaultSubscriptionService>();
        }
    }
}
