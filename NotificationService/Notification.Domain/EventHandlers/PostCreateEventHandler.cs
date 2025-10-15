using Blog.Contracts.Events;
using MessageBus.EventHandler;
using System.Net.Http.Json;

namespace Notification.Domain.EventHandlers
{
    public class PostCreateEventHandler : IEventHandler<PostUpdateEvent>
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PostCreateEventHandler(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task Handle(IMessageContext<PostUpdateEvent> @event)
        {
            if (@event.Message.UpdateType != UpdateType.Create)
            {
                return;
            }
            var post = @event.Message;
            const int pageSize = 1000;
            int page = 0;
            using var client = _httpClientFactory.CreateClient("Blog");
            do
            {
                try
                {
                    var subscribers = await client.GetFromJsonAsAsyncEnumerable<Guid>($"api/InternalPost/blogSubscribers/{post.BlogId}?page={page}&pageSize={pageSize}")
                        .ToListAsync();

                    foreach (var userId in subscribers)
                    {
                        Console.WriteLine(userId);
                    }
                    if (subscribers.Count != pageSize)
                        break;
                    page++;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            } while (true);
        }
    }
}
