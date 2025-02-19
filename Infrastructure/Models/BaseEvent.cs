namespace Infrastructure.Models
{
    public  class BaseEvent
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
        public required EventState State { get; set; }
        public required string EventType { get; set; }
        public required string EventData { get; set; } // Сериализованный JSON события
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; private set; }

        public void SetErrorMessage(string message)
        {
            ErrorMessage = message;
            State = EventState.Error;
        }
    }

    public enum EventState
    {
        Pending,
        Processed,
        Complete,
        Error
    }
}
