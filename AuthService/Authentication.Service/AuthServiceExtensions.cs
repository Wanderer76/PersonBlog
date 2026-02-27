using Authentication.Service.Service;
using Authentication.Service.Service.Implementation;
using Microsoft.Extensions.DependencyInjection;


namespace Authentication.Service;

public static class AuthServiceExtensions
{
    public static void AddAuthServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, DefaultAuthService>();
        services.AddScoped<ITokenService, DefaultTokenService>();
    }
}