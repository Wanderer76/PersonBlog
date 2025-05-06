using Blog.Domain.Events;
using MassTransit;

namespace Blog.API.Handlers
{
    public class VideoReadyToPublishEventHandler : IConsumer<VideoReadyToPublishEvent>
    {

        public Task Consume(ConsumeContext<VideoReadyToPublishEvent> context)
        {
            return Task.CompletedTask;
        }
    }
}
