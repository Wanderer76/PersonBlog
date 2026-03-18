using Amazon.S3;
using FileStorage.Service.Service;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileStorage.Service
{
    public static class FileStorageServiceExtensions
    {
        public static void AddFileStorage(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FileStorageOptions>(configuration.GetSection(nameof(FileStorageOptions)));
            services.AddScoped<IFileStorage, MinioFileStorage>();
            services.AddSingleton<IFileStorageFactory, DefaultFileStorageFactory>();
            services.AddSingleton<IAmazonS3>(services =>
            {
                var options = services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                return new AmazonS3Client(options.AccessKey, options.SecretKey, new AmazonS3Config
                {
                    ServiceURL = $"http://{options.Endpoint}",
                    UseHttp = true,
                });
            });
            services.AddScoped<IMultipartFileUpload, S3FileStorage>();
        }
    }
}
