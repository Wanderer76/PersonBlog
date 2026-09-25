namespace Profile.Application.Models;

public class SubscribeViewModel
{
    public Guid BlogId { get; }
    public DateTimeOffset CreatedAt { get; }

    public SubscribeViewModel(Guid blogId, DateTimeOffset createdAt)
    {
        BlogId = blogId;
        CreatedAt = createdAt;
    }
}
