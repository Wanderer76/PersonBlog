using Recommendation.Domain.Enums;
using Recommendation.Domain.Models;

namespace Recommendation.Domain.Entities;

public sealed class PostSnapshot : IRecommendationEntity
{
    private readonly List<PostCategory> _categories = [];

    public Guid PostId { get; private set; }
    public long SourceVersion { get; private set; }
    public Guid BlogId { get; private set; }
    public PostType PostType { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public PostVisibility Visibility { get; private set; }
    public PostProcessState ProcessState { get; private set; }
    public bool IsDeleted { get; private set; }
    public bool IsBanned { get; private set; }
    public Guid? PaymentSubscriptionId { get; private set; }
    public string? PreviewObjectName { get; private set; }
    public double? DurationSeconds { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int ViewCount { get; private set; }
    public int LikeCount { get; private set; }
    public int DislikeCount { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<PostCategory> Categories => _categories;

    public bool IsEligibleForPublicRecommendations =>
        Visibility == PostVisibility.Public
        && ProcessState == PostProcessState.Complete
        && !IsDeleted
        && !IsBanned
        && PaymentSubscriptionId is null;

    private PostSnapshot()
    {
    }

    private PostSnapshot(PostSnapshotData data)
    {
        PostId = data.PostId;
        Apply(data);
    }

    public static PostSnapshot Create(PostSnapshotData data)
    {
        Validate(data);
        return new PostSnapshot(data);
    }

    public bool TryApply(PostSnapshotData data)
    {
        Validate(data);
        if (data.PostId != PostId)
        {
            throw new ArgumentException("Snapshot belongs to another post.", nameof(data));
        }

        if (data.SourceVersion <= SourceVersion)
        {
            return false;
        }

        Apply(data);
        return true;
    }

    private void Apply(PostSnapshotData data)
    {
        SourceVersion = data.SourceVersion;
        BlogId = data.BlogId;
        PostType = data.PostType;
        Title = data.Title.Trim();
        Description = data.Description;
        Visibility = data.Visibility;
        ProcessState = data.ProcessState;
        IsDeleted = data.IsDeleted;
        IsBanned = data.IsBanned;
        PaymentSubscriptionId = data.PaymentSubscriptionId;
        PreviewObjectName = data.PreviewObjectName;
        DurationSeconds = data.DurationSeconds;
        CreatedAt = data.CreatedAt;
        ViewCount = data.ViewCount;
        LikeCount = data.LikeCount;
        DislikeCount = data.DislikeCount;
        UpdatedAt = data.UpdatedAt;

        _categories.Clear();
        _categories.AddRange(data.CategoryIds
            .Distinct()
            .Select(categoryId => new PostCategory(PostId, categoryId)));
    }

    private static void Validate(PostSnapshotData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.PostId == Guid.Empty) throw new ArgumentException("PostId is required.", nameof(data));
        if (data.BlogId == Guid.Empty) throw new ArgumentException("BlogId is required.", nameof(data));
        if (data.SourceVersion <= 0) throw new ArgumentOutOfRangeException(nameof(data), "SourceVersion must be positive.");
        if (string.IsNullOrWhiteSpace(data.Title)) throw new ArgumentException("Title is required.", nameof(data));
        if (data.DurationSeconds is < 0) throw new ArgumentOutOfRangeException(nameof(data), "Duration cannot be negative.");
        if (data.ViewCount < 0 || data.LikeCount < 0 || data.DislikeCount < 0)
            throw new ArgumentOutOfRangeException(nameof(data), "Engagement counters cannot be negative.");
        if (data.CategoryIds.Any(categoryId => categoryId <= 0))
            throw new ArgumentOutOfRangeException(nameof(data), "Category IDs must be positive.");
    }
}
