using Authentication.Contract.Services;
using Infrastructure.Extensions;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Authentication.Contract;

public class AuthenticationClientOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5179/api";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}

public static class AuthenticationContractExtensions
{
    public static void AddUserSessionServices(this IServiceCollection services, Action<AuthenticationClientOptions>? configureOptions = null)
    {
        var options = new AuthenticationClientOptions();
        configureOptions?.Invoke(options);
        services.AddHttpClient<HttpContextCachedUserService>("AuthApp", client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = options.Timeout;
        });

        services.AddScoped<ICurrentUserService>(sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpFactory.CreateClient("AuthApp");
            var httpUserService = new CurrentUserHttpClientService(httpClient);
            var contextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var cacheService = sp.GetRequiredService<ICacheService>();
            return new HttpContextCachedUserService(contextAccessor, cacheService, httpUserService);
        });

        services.AddHttpContextAccessor();
    }
}
