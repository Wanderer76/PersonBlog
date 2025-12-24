using Microsoft.Extensions.DependencyInjection;
using PlayListService.Services.Services;

namespace PlayListService.Services;
public static class PlayListServiceExtensions
{
    public static void AddPlayListService(this IServiceCollection services)
    {
        services.AddScoped<IPlayListService, CrudPlayListService>();
        services.AddScoped<PlayListFileService>();
    }
}
