using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MusicRecommendation.Services;

public static class MusicServiceExtensions
{
    public static void AddMusicRecommendationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<MusicRecommendationHttpClient>(x =>
        {
            x.BaseAddress = new Uri(configuration["AppUrls:MusicRecommendation"]);
        });
    }
}
