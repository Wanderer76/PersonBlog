using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Models
{
    public sealed class BaseEvent<T>
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

    public class BaseEventJsonConverter : JsonConverter<BaseEvent>
    {
        public override void Write(Utf8JsonWriter writer, BaseEvent value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(BaseEvent.Id), value.Id);
            writer.WriteString(nameof(BaseEvent.CorrelationId), value.CorrelationId?.ToString());
            writer.WriteString(nameof(BaseEvent.CreatedAt), value.CreatedAt.ToString("O"));
            writer.WriteString(nameof(BaseEvent.EventType), value.EventType);
            writer.WriteNumber(nameof(BaseEvent.RetryCount), value.RetryCount);
            writer.WriteString(nameof(BaseEvent.State), value.State.ToString());

            if (!string.IsNullOrWhiteSpace(value.ErrorMessage))
                writer.WriteString(nameof(BaseEvent.ErrorMessage), value.ErrorMessage);

            // Вставка EventData как raw JSON, без двойных кавычек
            writer.WritePropertyName(nameof(BaseEvent.EventData));
            using (var doc = JsonDocument.Parse(value.EventData))
            {
                doc.RootElement.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        public override BaseEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Десериализация не требуется в этом контексте");
        }
    }
}
