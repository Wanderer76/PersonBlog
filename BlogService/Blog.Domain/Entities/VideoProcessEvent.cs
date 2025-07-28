using Infrastructure.Models;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Text.Json;

namespace Blog.Domain.Entities
{
    public class VideoProcessEvent : BaseEvent, IBlogEntity
    {
        private VideoProcessEvent(string eventData, string eventType) : base(eventData, eventType)
        {
        }
        public static VideoProcessEvent Create<T>(T message, Guid? corellationId = null)
        {
            var data = JsonSerializer.Serialize(message);
            return new VideoProcessEvent(data, typeof(T).Name) { CorrelationId = corellationId };
        }
    }
}
