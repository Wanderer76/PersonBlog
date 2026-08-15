using Microsoft.EntityFrameworkCore;
using Recommendation.Domain.Entities;
using Recommendation.Domain.Enums;
using Recommendation.Services.Abstractions;
using Recommendation.Services.Models;
using Shared.Persistence;

namespace Recommendation.Persistence;

public sealed class EfRecommendationFeedStore(
    IReadWriteRepository<IRecommendationEntity> repository)
    : IRecommendationFeedStore
{
    public async Task<IReadOnlyList<RecommendationCandidateData>> LoadCandidatesAsync(
        Guid? userId,
        Guid? currentPostId,
        int limit,
        DateTimeOffset seenSince,
        CancellationToken cancellationToken = default)
    {
        var eligible = repository.Get<PostSnapshot>()
            .Where(x => x.Visibility == PostVisibility.Public)
            .Where(x => x.ProcessState == PostProcessState.Complete)
            .Where(x => !x.IsDeleted && !x.IsBanned)
            .Where(x => x.PaymentSubscriptionId == null)
            .Where(x => !currentPostId.HasValue || x.PostId != currentPostId.Value);

        var freshIds = await eligible
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.PostId)
            .Select(x => x.PostId)
            .Take(limit)
            .ToListAsync(cancellationToken);
        var trendingIds = await eligible
            .OrderByDescending(x => x.ViewCount + x.LikeCount * 2 - x.DislikeCount)
            .ThenByDescending(x => x.CreatedAt)
            .ThenBy(x => x.PostId)
            .Select(x => x.PostId)
            .Take(limit)
            .ToListAsync(cancellationToken);
        var candidateIds = freshIds
            .Zip(trendingIds, (fresh, trending) => new[] { fresh, trending })
            .SelectMany(x => x)
            .Concat(freshIds.Skip(trendingIds.Count))
            .Concat(trendingIds.Skip(freshIds.Count))
            .Distinct()
            .Take(limit)
            .ToArray();

        var posts = await repository.Get<PostSnapshot>()
            .Include(x => x.Categories)
            .Where(x => candidateIds.Contains(x.PostId))
            .ToListAsync(cancellationToken);

        var currentCategories = currentPostId.HasValue
            ? (await repository.Get<PostCategory>()
                .Where(x => x.PostId == currentPostId.Value)
                .Select(x => x.CategoryId)
                .ToListAsync(cancellationToken)).ToHashSet()
            : [];

        var categoryAffinities = new Dictionary<int, double>();
        var blogAffinities = new Dictionary<Guid, double>();
        var subscriptions = new HashSet<Guid>();
        var seenPosts = new HashSet<Guid>();
        if (userId.HasValue)
        {
            var categoryIds = posts.SelectMany(x => x.Categories).Select(x => x.CategoryId).Distinct().ToArray();
            categoryAffinities = await repository.Get<UserCategoryAffinity>()
                .Where(x => x.UserId == userId.Value && categoryIds.Contains(x.CategoryId))
                .ToDictionaryAsync(x => x.CategoryId, x => x.Score, cancellationToken);
            var blogIds = posts.Select(x => x.BlogId).Distinct().ToArray();
            blogAffinities = await repository.Get<UserBlogAffinity>()
                .Where(x => x.UserId == userId.Value && blogIds.Contains(x.BlogId))
                .ToDictionaryAsync(x => x.BlogId, x => x.Score, cancellationToken);
            subscriptions = (await repository.Get<UserSubscription>()
                .Where(x => x.UserId == userId.Value && blogIds.Contains(x.BlogId))
                .Select(x => x.BlogId)
                .ToListAsync(cancellationToken)).ToHashSet();
            seenPosts = (await repository.Get<UserInteraction>()
                .Where(x => x.UserId == userId.Value && x.OccurredAt >= seenSince)
                .Select(x => x.PostId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();
        }

        return posts.Select(post =>
        {
            var categories = post.Categories.Select(x => x.CategoryId).ToHashSet();
            var affinity = categories
                .Select(categoryId => categoryAffinities.GetValueOrDefault(categoryId))
                .DefaultIfEmpty()
                .Max();
            var intersection = currentCategories.Count == 0
                ? 0
                : categories.Count(currentCategories.Contains);
            var union = categories.Count + currentCategories.Count - intersection;
            var similarity = union == 0 ? 0 : intersection / (double)union;

            return new RecommendationCandidateData(
                post.PostId,
                post.BlogId,
                post.CreatedAt,
                post.ViewCount,
                post.LikeCount,
                post.DislikeCount,
                affinity,
                blogAffinities.GetValueOrDefault(post.BlogId),
                similarity,
                subscriptions.Contains(post.BlogId),
                seenPosts.Contains(post.PostId));
        }).ToArray();
    }

    public async Task SaveImpressionsAsync(
        IReadOnlyCollection<RecommendationImpression> impressions,
        CancellationToken cancellationToken = default)
    {
        if (impressions.Count == 0) return;
        var requestIds = impressions.Select(x => x.RequestId).Distinct().ToArray();
        var postIds = impressions.Select(x => x.PostId).Distinct().ToArray();
        var existing = (await repository.Get<RecommendationImpression>()
            .Where(x => requestIds.Contains(x.RequestId) && postIds.Contains(x.PostId))
            .Select(x => new { x.RequestId, x.PostId })
            .ToListAsync(cancellationToken))
            .Select(x => (x.RequestId, x.PostId))
            .ToHashSet();
        var newImpressions = impressions
            .Where(x => !existing.Contains((x.RequestId, x.PostId)))
            .ToArray();
        if (newImpressions.Length == 0) return;
        foreach (var impression in newImpressions)
        {
            repository.Add(impression);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await repository.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<RecommendationPostSummary>> LoadPostSummariesAsync(
        IReadOnlyCollection<Guid> postIds,
        CancellationToken cancellationToken = default)
    {
        var posts = await repository.Get<PostSnapshot>()
            .Where(x => postIds.Contains(x.PostId))
            .Where(x => x.Visibility == PostVisibility.Public)
            .Where(x => x.ProcessState == PostProcessState.Complete)
            .Where(x => !x.IsDeleted && !x.IsBanned && x.PaymentSubscriptionId == null)
            .Select(x => new
            {
                x.PostId,
                x.BlogId,
                x.PostType,
                x.Title,
                x.Description,
                x.PreviewObjectName,
                x.DurationSeconds,
                x.ViewCount,
                x.LikeCount,
                x.DislikeCount,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return posts.Select(x => new RecommendationPostSummary(
            x.PostId,
            x.BlogId,
            x.PostType.ToString(),
            x.Title,
            x.Description,
            x.PreviewObjectName,
            x.DurationSeconds,
            x.ViewCount,
            x.LikeCount,
            x.DislikeCount,
            x.CreatedAt)).ToArray();
    }
}
