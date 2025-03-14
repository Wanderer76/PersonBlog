using Authentication.Domain.Interfaces;
using Authentication.Service.Service;
using Authentication.Service.Service.Implementation;
using AuthenticationApplication.Service;
using AuthenticationApplication.Service.ApiClient;
using Microsoft.Extensions.DependencyInjection;


namespace Authentication.Service
{
    public static class AuthServiceExtensions
    {
        public static void AddAuthServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, DefaultAuthService>();
            services.AddTransient<IProfileApiAsyncClient, DefaultProfileApiAsyncClient>();
            services.AddScoped<ITokenService, DefaultTokenService>();

        }

        public static void AddUserSessionServices(this IServiceCollection services)
        {
            services.AddScoped<IUserSession, DefaultUserSession>();
            services.AddHttpContextAccessor();
        }
    }
}
