using Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Authentication.Domain.Entities
{
    public class AuthEvent : BaseEvent, IAuthEntity
    {
        private AuthEvent(string eventData, string eventType)
            : base(eventData, eventType)
        {
        }

        public static AuthEvent Create<T>(T message)
        {
            var data = JsonSerializer.Serialize(message);
            return new AuthEvent(data, typeof(T).Name);
        }
    }
}
