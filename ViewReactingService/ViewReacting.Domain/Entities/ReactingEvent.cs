using MessageBus.Models;
using Shared.Services;
using System.Text.Json;

namespace ViewReacting.Domain.Entities
{
    public class ReactingEvent : BaseEvent, IUserEntity
    {
        private ReactingEvent(string eventData, string eventType)
            : base(GuidService.GetNewGuid(), null, eventData, eventType)
        {
        }

        private ReactingEvent(Guid? correlationId, string eventData, string eventType)
           : this(GuidService.GetNewGuid(), correlationId, eventData, eventType)
        {
        }

        private ReactingEvent(Guid id, Guid? correlationId, string eventData, string eventType)
           : base(id, correlationId, eventData, eventType)
        {
        }

        public static ReactingEvent Create<T>(T message, Guid? id, Guid? correlationId = null)
        {
            var data = JsonSerializer.Serialize(message);
            id ??= GuidService.GetNewGuid();
            return new ReactingEvent(id.Value, correlationId, data, typeof(T).Name);
        }
    }
}
