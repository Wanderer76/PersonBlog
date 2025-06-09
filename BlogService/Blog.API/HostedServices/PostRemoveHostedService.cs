
using Blog.Domain.Entities;
using FileStorage.Service.Service;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Blog.API.HostedServices
{
    public class PostRemoveHostedService : IHostedService
    {
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly IServiceProvider _serviceProvider;

        public PostRemoveHostedService(IServiceProvider serviceProvider, IFileStorageFactory fileStorageFactory)
        {
            _serviceProvider = serviceProvider;
            _fileStorageFactory = fileStorageFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = HandlePostRemove(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task HandlePostRemove(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IBlogEntity>>();

            while (!cancellationToken.IsCancellationRequested)
            {
                var date = DateTimeService.Now().AddDays(-2);

                var posts  = await repository.Get<PostRemoveEvent>()
                    .Where(x => x.DeletedAt <= date)
                    .Take(10)
                    .ToListAsync();
                using var storage = _fileStorageFactory.CreateFileStorage();
                foreach (var post in posts)
                {
                    await storage.RemoveBucketAsync(post.Id.ToString());
                    repository.Remove(post);
                }
                await repository.SaveChangesAsync();
            }
        }
    }
}
