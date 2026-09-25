using Infrastructure.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Profile.Application.Services;
using Profile.Service.Implementation;

namespace Profile.Service
{
    public static class ProfileServiceExtensions
    {
        public static void AddProfileServices(this IServiceCollection services)
        {
            services.AddScoped<IViewHistoryService, DefaultViewHistoryService>();
            services.AddScoped<IReactionService, DefaultReactionService>();
            services.AddScoped<ISubscribeService, DefaultSubscriptionService>();
            services.AddScoped<IProfileService, DefaultProfileService>();
            services.AddScoped<IBanService, DefaultPostBanService>();
            services.AddScoped<IProfilePictureStore, ProfilePictureStore>();
        }
       
    }
}
