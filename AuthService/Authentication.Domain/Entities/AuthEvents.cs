using MessageBus.Models;
using Shared.Services;
using System.Text.Json;

namespace Authentication.Domain.Entities
{
    public class AuthEvent : BaseEvent, IAuthEntity
    {
        private AuthEvent(string eventData, string eventType)
            : base(GuidService.GetNewGuid(), null, eventData, eventType)
        {
        }
        private AuthEvent(Guid? correlationId, string eventData, string eventType)
            : base(GuidService.GetNewGuid(), correlationId, eventData, eventType)
        {
        }

        public static AuthEvent Create<T>(T message, Guid? correlationId = null)
        {
            var data = JsonSerializer.Serialize(message);
            return new AuthEvent(correlationId, data, typeof(T).Name);
        }
    }
}
