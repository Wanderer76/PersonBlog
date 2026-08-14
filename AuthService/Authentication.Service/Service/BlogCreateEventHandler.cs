using Blog.Contracts.Events;
using MessageBus.EventHandler;

namespace Authentication.Service.Service;

public sealed class BlogCreateEventHandler(IBlogUserProvisioningService provisioningService)
    : IEventHandler<BlogCreateEvent>
{
    public async Task Handle(IMessageContext<BlogCreateEvent> @event)
    {
        await provisioningService.ProvisionBlogAsync(
            @event.Message.UserId,
            @event.Message.BlogId);
    }
}
