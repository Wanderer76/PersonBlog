using System.Text.Json;
using MessageBus.Models;

namespace Comments.Domain.Entities;

public sealed class CommentOutboxMessage : BaseEvent, ICommentEntity
{
    public const int MaxPublishAttempts = 5;

    private CommentOutboxMessage(Guid id, string eventData, string eventType)
        : base(id, null, eventData, eventType) { }

    public static CommentOutboxMessage Create<T>(T message, Guid eventId)
        => new(eventId, JsonSerializer.Serialize(message), typeof(T).Name);

    public void RegisterPublishFailure(string error)
    {
        RetryCount++;
        if (RetryCount >= MaxPublishAttempts) SetErrorMessage(error);
        else ResetEvent();
    }
}
