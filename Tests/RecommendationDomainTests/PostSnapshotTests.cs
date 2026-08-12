using Recommendation.Domain.Entities;
using Recommendation.Domain.Enums;
using Recommendation.Domain.Models;

namespace RecommendationDomainTests;

public sealed class PostSnapshotTests
{
    [Fact]
    public void Create_normalizes_categories_and_exposes_eligibility()
    {
        var snapshot = PostSnapshot.Create(CreateData(categoryIds: [2, 1, 2]));

        Assert.True(snapshot.IsEligibleForPublicRecommendations);
        Assert.Equal([2, 1], snapshot.Categories.Select(x => x.CategoryId));
    }

    [Fact]
    public void TryApply_ignores_same_or_older_source_version()
    {
        var snapshot = PostSnapshot.Create(CreateData(sourceVersion: 2));

        var applied = snapshot.TryApply(CreateData(sourceVersion: 1, title: "Stale"));

        Assert.False(applied);
        Assert.Equal("Post", snapshot.Title);
        Assert.Equal(2, snapshot.SourceVersion);
    }

    [Fact]
    public void TryApply_replaces_snapshot_and_categories()
    {
        var snapshot = PostSnapshot.Create(CreateData(categoryIds: [1, 2]));

        var applied = snapshot.TryApply(CreateData(
            sourceVersion: 2,
            title: "Updated",
            categoryIds: [3],
            isBanned: true));

        Assert.True(applied);
        Assert.Equal("Updated", snapshot.Title);
        Assert.Equal([3], snapshot.Categories.Select(x => x.CategoryId));
        Assert.False(snapshot.IsEligibleForPublicRecommendations);
    }

    private static PostSnapshotData CreateData(
        long sourceVersion = 1,
        string title = "Post",
        IReadOnlyCollection<int>? categoryIds = null,
        bool isBanned = false) => new()
    {
        PostId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
        SourceVersion = sourceVersion,
        BlogId = Guid.Parse("20000000-0000-0000-0000-000000000002"),
        PostType = PostType.Video,
        Title = title,
        CategoryIds = categoryIds ?? [1],
        Visibility = PostVisibility.Public,
        ProcessState = PostProcessState.Complete,
        IsDeleted = false,
        IsBanned = isBanned,
        CreatedAt = DateTimeOffset.Parse("2026-08-12T10:00:00Z"),
        ViewCount = 10,
        LikeCount = 3,
        DislikeCount = 1,
        UpdatedAt = DateTimeOffset.Parse("2026-08-12T11:00:00Z")
    };
}
