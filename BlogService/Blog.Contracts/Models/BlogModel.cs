namespace Blog.Contracts.Models.Blog;

public class BlogModel
{
    public Guid Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public DateTimeOffset CreatedAt { get; }
    public string? PhotoUrl { get; }
    public Guid UserId { get; }

    public int SubscribersCount { get; }

    public BlogModel(Guid id, string name, string? description, DateTimeOffset createdAt, string? photoUrl, Guid userId, int subscribersCount)
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
        PhotoUrl = photoUrl;
        UserId = userId;
        SubscribersCount = subscribersCount;
    }
}
