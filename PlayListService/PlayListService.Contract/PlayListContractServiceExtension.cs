using Infrastructure.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlayListService.Services.Services;

namespace PlayListService.Contract;
public static class PlayListContractServiceExtension
{
    public static void AddPlayListContract(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<HeaderClientHandler>();

        services.AddHttpClient<IPlayListService, PlaylistHttpApiClient>(cfg =>
        {
            cfg.BaseAddress = new Uri(configuration["AppUrls:PlayList"]!);
        }).AddHttpMessageHandler<HeaderClientHandler>();
    }
}
