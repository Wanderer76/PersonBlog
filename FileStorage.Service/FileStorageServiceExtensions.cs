using FileStorage.Service.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileStorage.Service
{
    public static class FileStorageServiceExtensions
    {
        public static void AddFileStorage(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FileStorageOptions>(configuration.GetSection(nameof(FileStorageOptions)));
            services.AddScoped<IFileStorage, MinioFileStorage>();
            services.AddSingleton<IFileStorageFactory, DefaultFileStorageFactory>();
        }
    }
}
