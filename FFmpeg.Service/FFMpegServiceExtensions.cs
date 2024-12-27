using Microsoft.Extensions.DependencyInjection;

namespace FFmpeg.Service
{
    public static class FFMpegServiceExtensions
    {
        public static void AddFFMpeg(this IServiceCollection service)
        {
            //var defaultOptions = options ?? new FFOptions { BinaryFolder = "../usr/bin", TemporaryFilesFolder = "../tmp" };
            //GlobalFFOptions.Configure(defaultOptions);
        }
    }
}
