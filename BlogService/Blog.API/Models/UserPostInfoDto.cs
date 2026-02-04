using Blog.Domain.Entities;

namespace Blog.API.Models;

public class UserPostInfoDto
{
    public Guid Id { get; set; }
    public Guid BlogId { get; set; }
    public int DislikeCount { get; set; }
    public int LikeCount { get; set; }
    public long ViewCount { get; set; }
    public Guid? PaymentSubscriptionId { get; set; }
    public PostVisibility Visibility { get; set; }
    public string Title { get; set; } = default!;

    public TextInfoDto? TextInfo { get; set; }
    public VideoInfoDto? VideoInfo { get; set; }
    public DateTimeOffset CreatedAt { get; internal set; }
}
