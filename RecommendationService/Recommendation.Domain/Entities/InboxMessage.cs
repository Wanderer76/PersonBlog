namespace Recommendation.Domain.Entities;

public sealed class InboxMessage : IRecommendationEntity
{
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = null!;
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    public bool IsProcessed => ProcessedAt.HasValue;

    private InboxMessage()
    {
    }

    public InboxMessage(Guid eventId, string eventType, DateTimeOffset receivedAt)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("EventId is required.", nameof(eventId));
        if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("EventType is required.", nameof(eventType));
        EventId = eventId;
        EventType = eventType.Trim();
        ReceivedAt = receivedAt;
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        if (processedAt < ReceivedAt) throw new ArgumentOutOfRangeException(nameof(processedAt));
        ProcessedAt = processedAt;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        if (IsProcessed) throw new InvalidOperationException("A processed inbox message cannot fail.");
        if (string.IsNullOrWhiteSpace(error)) throw new ArgumentException("Error is required.", nameof(error));
        RetryCount++;
        LastError = error.Trim();
    }
}
