using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services
{
    public interface IFileStorageFactory
    {
        IFileStorage CreateFileStorage();
    }

    public class DefaultFileStorageFactory : IFileStorageFactory
    {
        private readonly IServiceScopeFactory _serviceProvider;

        public DefaultFileStorageFactory(IServiceScopeFactory serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IFileStorage CreateFileStorage()
        {
            return _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IFileStorage>();
        }
    }
}
