using System.Text.Json;
using MessageBus.Models;

namespace Conference.Domain.Entities;

public sealed class ConferenceOutboxMessage : BaseEvent, IConferenceEntity
{
    public const int MaxPublishAttempts = 5;

    private ConferenceOutboxMessage(Guid id, string eventData, string eventType)
        : base(id, null, eventData, eventType) { }

    public static ConferenceOutboxMessage Create<T>(T message, Guid eventId)
        => new(eventId, JsonSerializer.Serialize(message), typeof(T).Name);

    public void RegisterPublishFailure(string error)
    {
        RetryCount++;
        if (RetryCount >= MaxPublishAttempts) SetErrorMessage(error);
        else ResetEvent();
    }
}
