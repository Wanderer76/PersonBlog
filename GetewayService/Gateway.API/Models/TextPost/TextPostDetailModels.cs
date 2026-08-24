namespace Gateway.API.Models.TextPost;

public sealed record TextPostDetailResponse(
    Guid Id,
    string Title,
    string? Lead,
    string Text,
    DateTimeOffset CreatedAt,
    int EstimatedReadingTimeMinutes,
    long ViewCount,
    int LikeCount,
    int DislikeCount,
    IReadOnlyList<TextPostCategory> Categories,
    IReadOnlyList<TextPostMedia> Media,
    TextPostAuthor Author,
    TextPostViewerState Viewer);

public sealed record TextPostCategory(
    int Id,
    string Title);

public sealed record TextPostMedia(
    Guid Id,
    string Name,
    string Url,
    string ContentType,
    long Length);

public sealed record TextPostAuthor(
    Guid BlogId,
    string Name,
    string? Description,
    string? PhotoUrl,
    int SubscribersCount,
    int PostsCount,
    long TotalViewsCount);

public sealed record TextPostViewerState(
    bool IsViewed,
    bool? IsLike,
    bool IsSubscribed,
    bool CanEdit,
    bool CanDelete);
