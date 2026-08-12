using Blog.Domain.Entities;
using Recommendation.Contracts.Events;
using Shared.Services;

namespace Blog.Service.Events;

internal static class PostCatalogChangedV2Factory
{
    public static PostCatalogChangedV2 Create(Post post, DateTimeOffset? occurredAt = null)
    {
        var videoInfo = post.Type == PostType.Video ? post.VideoPostInfo : null;

        return new PostCatalogChangedV2
        {
            EventId = GuidService.GetNewGuid(),
            AggregateVersion = post.RecommendationVersion,
            OccurredAt = occurredAt ?? DateTimeService.Now(),
            PostId = post.Id,
            BlogId = post.BlogId,
            PostType = (RecommendationPostType)post.Type,
            Title = post.Title,
            Description = post.Type == PostType.Video
                ? videoInfo?.Description
                : post.TextPostInfo?.Text,
            CategoryIds = videoInfo?.PostCategories.Select(x => x.CategoryId).ToArray() ?? [],
            Visibility = (RecommendationPostVisibility)post.Visibility,
            ProcessState = (RecommendationProcessState)post.ProcessState,
            IsDeleted = post.IsDelete,
            IsBanned = post.BanMessageId.HasValue,
            PaymentSubscriptionId = post.PaymentSubscriptionId,
            PreviewObjectName = videoInfo?.PreviewFile?.ObjectName,
            DurationSeconds = videoInfo?.VideoFile?.Duration,
            CreatedAt = post.CreatedAt,
            ViewCount = post.ViewCount,
            LikeCount = post.LikeCount,
            DislikeCount = post.DislikeCount
        };
    }
}
