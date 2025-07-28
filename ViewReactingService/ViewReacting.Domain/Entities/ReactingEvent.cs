using Infrastructure.Models;
using Shared.Services;
using System.Text.Json;

namespace ViewReacting.Domain.Entities
{
    public class ReactingEvent : BaseEvent, IUserEntity
    {
        public ReactingEvent(string eventData, string eventType) : base(eventData, eventType)
        {
        }

        public static ReactingEvent Create<T>(T message, Guid? id, Guid? corellationId = null)
        {
            var data = JsonSerializer.Serialize(message);
            return new ReactingEvent(data, typeof(T).Name) { Id = id ?? GuidService.GetNewGuid(), CorrelationId = corellationId };
        }
    }
}
