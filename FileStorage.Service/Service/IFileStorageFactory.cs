using Microsoft.Extensions.Configuration;

namespace FileStorage.Service.Service
{
    public interface IFileStorageFactory
    {
        IFileStorage CreateFileStorage();
    }

    internal class DefaultFileStorageFactory : IFileStorageFactory
    {
        private readonly IConfiguration configuration;
        private readonly FileStorageOptions options = new FileStorageOptions();

        public DefaultFileStorageFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
            configuration.GetRequiredSection(nameof(FileStorageOptions)).Bind(options);
        }

        public IFileStorage CreateFileStorage()
        {
            return new MinioFileStorage(options);
        }
    }
}
