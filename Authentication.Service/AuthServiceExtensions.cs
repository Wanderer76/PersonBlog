using Authentication.Domain.Interfaces;
using Authentication.Service.Service;
using Authentication.Service.Service.Implementation;
using AuthenticationApplication.Service;
using Microsoft.Extensions.DependencyInjection;


namespace Authentication.Service
{
    public static class AuthServiceExtensions
    {
        public static void AddAuthServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, DefaultAuthService>();
            services.AddScoped<IProfileService, DefaultProfileService>();
            services.AddScoped<ITokenService, DefaultTokenService>();
        }
    }
}
