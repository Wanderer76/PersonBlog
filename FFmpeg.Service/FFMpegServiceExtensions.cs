using FFmpeg.Service.Internal;
using FFmpeg.Service.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FFmpeg.Service
{
    public static class FFMpegServiceExtensions
    {
        public static void AddFFMpeg(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddSingleton<IFFMpegService, FFMpegService>();
            service.AddSingleton<FFMpegOptions>(configuration.GetSection("FFMpegOptions:FFMpeg").Get<FFMpegOptions>()!);
            service.AddSingleton<HlsVideoPresets>(configuration.GetSection("FFMpegOptions:HlsVideoPresets").Get<HlsVideoPresets>()!);
        }
    }
}
