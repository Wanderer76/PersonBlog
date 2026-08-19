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
            services.AddOptions<FileStorageOptions>()
                .Bind(configuration.GetSection(nameof(FileStorageOptions)))
                .Validate(options => !string.IsNullOrWhiteSpace(options.Endpoint), "File storage endpoint is required")
                .Validate(options => !string.IsNullOrWhiteSpace(options.AccessKey), "File storage access key is required")
                .Validate(options => !string.IsNullOrWhiteSpace(options.SecretKey), "File storage secret key is required")
                .Validate(
                    options => options.PresignedUrlExpirySeconds is >= 1 and <= 3600,
                    $"{nameof(FileStorageOptions.PresignedUrlExpirySeconds)} must be between 1 and 3600 seconds")
                .ValidateOnStart();
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
            services.AddScoped<IMultipartFileUpload, S3MultipartFileUploadService>();
        }
    }
}
