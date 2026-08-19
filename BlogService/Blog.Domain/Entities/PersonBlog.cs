using Shared.Utils;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Blog.Domain.Entities;

public class PersonBlog : IBlogEntity
{
    [Key]
    public Guid Id { get; private set; }
    [Required]
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? PhotoUrl { get; private set; }
    public Guid UserId { get; private set; }
    public int SubscriptionsCount { get; private set; }
    public List<Subscriber> Subscriptions { get; private set; } = [];

    internal PersonBlog() { }
    [JsonConstructor]
    internal PersonBlog(Guid id, string title, string? description, DateTimeOffset createdAt, string? photoUrl, Guid userId, int subscriptionsCount, List<Subscriber> subscriptions)
    {
        Id = id;
        Title = title;
        Description = description;
        CreatedAt = createdAt;
        PhotoUrl = photoUrl;
        UserId = userId;
        SubscriptionsCount = subscriptionsCount;
        Subscriptions = subscriptions;
    }

    public static Result<PersonBlog> CreateBlog(
        Guid id,
        DateTimeOffset createdAt,
        string title,
        string? description,
        string? photoId,
        Guid creatorId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result<PersonBlog>.Failure(new Error(nameof(title), "Blog title is empty"));
        }

        var blog = new PersonBlog(
            id,
            title,
            description,
            createdAt,
            photoId,
            creatorId,
            0,
            []);

        return blog;
    }

    public Result Update(string title, string? description, string? photoUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure(new Error(nameof(title), "Blog title is empty"));
        }

        Title = title.Trim();
        Description = description?.Trim();
        PhotoUrl = photoUrl;
        return Result.Success();
    }
}
