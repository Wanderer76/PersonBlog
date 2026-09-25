using MessageBus.Models;
using Microsoft.AspNetCore.Authentication.OAuth;
using Shared.Services;
using System.Text.Json;

namespace Blog.Domain.Entities
{
    public class VideoProcessEvent : BaseEvent, IBlogEntity
    {
        public const int MaxPublishAttempts = 3;

        private VideoProcessEvent(string eventData, string eventType)
            : base(GuidService.GetNewGuid(), null, eventData, eventType)
        {
        }

        private VideoProcessEvent(Guid? correlationId, string eventData, string eventType)
            : base(GuidService.GetNewGuid(), correlationId, eventData, eventType)
        {
        }

        public static VideoProcessEvent Create<T>(T message, Guid? corellationId = null)
        {
            var data = JsonSerializer.Serialize(message);
            return new VideoProcessEvent(corellationId, data, typeof(T).Name);
        }

        public void RegisterPublishFailure(string errorMessage)
        {
            RetryCount++;
            if (RetryCount >= MaxPublishAttempts)
            {
                SetErrorMessage(errorMessage);
                return;
            }

            ResetEvent();
        }
    }
}
