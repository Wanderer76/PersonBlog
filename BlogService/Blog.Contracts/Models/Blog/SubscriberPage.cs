namespace Blog.Contracts.Models.Blog;

public sealed record SubscriberPage(IReadOnlyList<Guid> UserIds, string? NextCursor, bool HasMore);
