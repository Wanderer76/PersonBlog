using MessageBus.Models;
using Shared.Services;
using System.Text.Json;

namespace Music.Domain.Entities
{
    public class MusicEvents : BaseEvent, IMusicEntity
    {
        private MusicEvents(Guid id, Guid? correlationId, string eventData, string eventType) : base(id, correlationId, eventData, eventType)
        {
        }

        public static MusicEvents Create<T>(T message, Guid? corellationId = null)
        {
            var data = JsonSerializer.Serialize(message);
            return new MusicEvents(GuidService.GetNewGuid(), corellationId, data, typeof(T).Name);
        }
    }
}
