using Blog.Contracts.Events;
using Blog.Domain.Entities;
using Shared.Services;

namespace Blog.Service.Events;

internal static class PostPublishedV1Factory
{
    public static PostPublishedV1? TryCreate(Post post, Guid authorUserId, DateTimeOffset? publishedAt = null)
    {
        var occurredAt = (publishedAt ?? DateTimeService.Now()).ToUniversalTime();
        if (!post.TryMarkPublished(GuidService.GetNewGuid(), occurredAt)) return null;
        return new PostPublishedV1
        {
            EventId = GuidService.GetNewGuid(),
            OccurredAt = occurredAt,
            PostId = post.Id,
            BlogId = post.BlogId,
            AuthorUserId = authorUserId,
            PublicationId = post.PublicationId!.Value,
            PublishedAt = post.PublishedAt!.Value,
            Title = post.Title,
            Audience = post.Visibility switch
            {
                PostVisibility.Public => PostPublicationAudience.Public,
                PostVisibility.ByUrl => PostPublicationAudience.ByUrl,
                _ => PostPublicationAudience.Private
            }
        };
    }
}
