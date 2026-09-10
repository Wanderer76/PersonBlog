using Blog.Contracts.Models.Blog;

namespace Blog.Contracts.Services;

public interface ISubscriberDirectoryService
{
    Task<Result<SubscriberPage>> GetPageAsync(Guid blogId, string? cursor, int limit,
        DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
