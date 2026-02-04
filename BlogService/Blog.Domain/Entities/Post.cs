using Infrastructure.Interface;
using Shared.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities;

public sealed class Post : IBlogEntity, ISoftDelete
{
    [Key]
    public Guid Id { get; private set; }
    public Guid BlogId { get; private set; }
    public PostType Type { get; set; }
    public DateTimeOffset CreatedAt { get; private set; }
    [Required]
    public string Title { get; set; }
    public Guid? PaymentSubscriptionId { get; set; }
    public int ViewCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public int DislikeCount { get; set; } = 0;
    public PostVisibility Visibility { get; set; }
    public ProcessState ProcessState { get; set; }

    public VideoPostInfo VideoPostInfo { get; set; }
    public TextPostInfo TextPostInfo { get; set; }
    public bool IsDelete { get; private set; }
    public DateTimeOffset? DeleteDateTime { get; private set; }

    [ForeignKey(nameof(BlogId))]
    public PersonBlog Blog { get; set; }

    public Guid? BanMessageId { get; private set; }
    public BanMessage? BanMessage { get; private set; }

    private Post() { }

    public Post(Guid id, Guid blogId, PostType type, string? description, string title, Guid? paymentSubscriptionId, PostVisibility visibility, IEnumerable<int> categories, string? text)
    {
        Id = id;
        BlogId = blogId;
        Type = type;
        CreatedAt = DateTimeService.Now();
        IsDelete = false;
        Title = title;
        PaymentSubscriptionId = paymentSubscriptionId;
        Visibility = visibility;
        if (type == PostType.Video)
        {
            VideoPostInfo = new VideoPostInfo
            {
                Id = id,
                PostCategories = categories.Select(x => new PostCategory(Id, x)).ToList(),
                Description = description,
            };
        }
        else
        {
            TextPostInfo = new TextPostInfo(id, text.Trim(), []);
        }
    }

    public void AddCategory(Category categories)
    {
        if (!VideoPostInfo.PostCategories.Any(x => x.CategoryId == categories.Id))
        {
            VideoPostInfo.PostCategories.Add(new PostCategory(Id, categories.Id));
        }
    }

    public void SetPostBanned(BanMessage message)
    {
        BanMessageId = message.Id;
        BanMessage = message;
    }

    public void RestorePostFromBan()
    {
        BanMessageId = null;
        BanMessage = null;
    }

    public void Delete()
    {
        IsDelete = true;
        DeleteDateTime = DateTimeService.Now();
    }
}

public enum PostType
{
    Text,
    Video
}
public enum ProcessState
{
    Draft,
    Complete,
    Load,
    Error
}

public static class ProcessStateExtensions
{
    public static bool IsComplete(this ProcessState state) { return state == ProcessState.Complete; }
    public static bool IsProcessComplete(this Post file) { return file.ProcessState.IsComplete(); }
}
