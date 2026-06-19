using FFmpeg.Service;
using Infrastructure.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediaProcessing.Contract;

public static class MediaProcessingExtensions
{
    public static void AddMediaProcessingContract(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<HeaderClientHandler>();
        services.AddHttpClient<IImageConvertService, ImageConverterService>(x =>
        {
            x.BaseAddress = new Uri(configuration["AppUrls:VideoProcessing"]!);
        }).AddHttpMessageHandler<HeaderClientHandler>();
    }
}
