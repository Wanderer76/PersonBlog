using FFmpeg.Service;
using FFMpegCore;
using Microsoft.Extensions.DependencyInjection;
using Profile.Service.Interface;
using Profile.Service.Interface.Implementation;

namespace Profile.Service.Extensions
{
    public static class ProfileServiceExtensions
    {
        public static void AddProfileServices(this IServiceCollection services)
        {
            services.AddScoped<IProfileService, DefaultProfileService>();
            services.AddScoped<IPostService,DefaultPostService>();
            services.AddScoped<IBlogService, DefaultBlogService>();
            //services.AddFFMpeg(new FFOptions
            //{
            //    BinaryFolder = "../ffmpeg",
                
            //});
        }
    }
}
