﻿namespace Infrastructure.Models
{
    public class BaseEvent<T>
    {
        public required string EventType { get; set; }
        public required T EventData { get; set; }
    }

    public abstract class BaseEvent
    {
        public Guid Id { get; set; }
        public Guid? CorrelationId {  get; set; }
        public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
        public EventState State { get => _state; set => _state = value; }
        public required string EventType { get; set; }
        public required string EventData { get; set; } // Сериализованный JSON события
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; private set; }

        private EventState _state = EventState.Pending;

        public void SetErrorMessage(string message)
        {
            ErrorMessage = message;
            _state = EventState.Error;
        }

        public void Complete()
        {
            _state = EventState.Complete;
        }
        public void ResetEvent()
        {
            _state = EventState.Pending;
        }
        public void Processed()
        {
            _state = EventState.Processed;
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
