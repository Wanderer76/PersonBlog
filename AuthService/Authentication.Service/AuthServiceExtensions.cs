using Authentication.Service.Models.Options;
using Authentication.Service.Service;
using Authentication.Service.Service.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Authentication.Service;

public static class AuthServiceExtensions
{
    public static void AddAuthServices(this IServiceCollection services,TokenOptions tokenOptions)
    {
        services.AddSingleton<TokenOptions>(x => tokenOptions);
        services.AddScoped<IAuthService, DefaultAuthService>();
        services.AddScoped<IUserContextService, DefaultUserContextService>();
        services.AddScoped<IOAuthService, DefaultOAuthService>();
        services.AddScoped<ITokenService, DefaultTokenService>();
    }
}
