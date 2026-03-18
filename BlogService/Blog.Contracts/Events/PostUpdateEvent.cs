using MessageBus;

namespace Blog.Contracts.Events;

[EventPublish(Exchange = "post-update")]
public class PostUpdateEvent
{
    public Guid PostId { get; set; }
    public Guid BlogId { get; set; }
    public int ViewCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public UpdateType UpdateType { get; set; }
}

public enum UpdateType
{
    Create,
    Update,
    Delete,
}
