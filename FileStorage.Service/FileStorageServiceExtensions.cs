using FileStorage.Service.Service;
using Microsoft.Extensions.DependencyInjection;

namespace FileStorage.Service
{
    public static class FileStorageServiceExtensions
    {
        public static void AddFileStorage(this IServiceCollection services)
        {
            services.AddSingleton<IFileStorageFactory, DefaultFileStorageFactory>();
        }
    }
}
