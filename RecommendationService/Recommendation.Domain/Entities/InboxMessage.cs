using Shared.Utils;

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

    private InboxMessage(Guid eventId, string eventType, DateTimeOffset receivedAt)
    {
        EventId = eventId;
        EventType = eventType.Trim();
        ReceivedAt = receivedAt;
    }

    public static Result<InboxMessage> Create(Guid eventId, string eventType, DateTimeOffset receivedAt)
    {
        if (eventId == Guid.Empty)
            return new Error(nameof(eventId), "EventId is required.");
        if (string.IsNullOrWhiteSpace(eventType))
            return new Error(nameof(eventType), "EventType is required.");

        return new InboxMessage(eventId, eventType, receivedAt);
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
