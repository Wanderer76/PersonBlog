using Blog.Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Persistence;

namespace Blog.Service.Services.Implementation;

public sealed class PostFileCleanupService(
    IReadWriteRepository<IBlogEntity> repository,
    IFileStorageFactory fileStorageFactory,
    ILogger<PostFileCleanupService> logger)
{
    public async Task<int> CleanupExpiredAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default)
    {
        var events = await repository.Get<PostRemoveEvent>()
            .Where(item => item.DeletedAt <= cutoff)
            .OrderBy(item => item.DeletedAt)
            .Take(10)
            .ToListAsync(cancellationToken);
        var cleanedCount = 0;

        using var storage = fileStorageFactory.CreateFileStorage();
        foreach (var removeEvent in events)
        {
            try
            {
                var blogId = await repository.Get<Post>()
                    .Where(post => post.Id == removeEvent.PostId && post.IsDelete)
                    .Select(post => (Guid?)post.BlogId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (blogId.HasValue)
                {
                    await storage.RemoveFilesByPrefixAsync(
                        blogId.Value,
                        $"{removeEvent.PostId}/",
                        cancellationToken);
                }

                repository.Remove(removeEvent);
                await repository.SaveChangesAsync();
                cleanedCount++;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(
                    exception,
                    "Failed to remove files for deleted post {PostId}",
                    removeEvent.PostId);
            }
        }

        return cleanedCount;
    }
}
