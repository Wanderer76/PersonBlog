namespace Infrastructure.Models
{
    public abstract class BaseEvent
    {
        public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
        public required EventState State { get; set; }
    }

    public enum EventState
    {
        New,
        Complete,
        Error
    }
}
