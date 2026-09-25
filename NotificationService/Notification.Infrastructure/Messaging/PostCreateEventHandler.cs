using Blog.Contracts.Events;
using MessageBus.EventHandler;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Notification.Infrastructure.Messaging
{
    public class PostCreateEventHandler : IEventHandler<PostUpdateEvent>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PostCreateEventHandler> _logger;

        public PostCreateEventHandler(
            IHttpClientFactory httpClientFactory,
            ILogger<PostCreateEventHandler> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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
                        _logger.LogDebug("Post notification recipient: {UserId}", userId);
                    }
                    if (subscribers.Count != pageSize)
                        break;
                    page++;
                }
                catch (HttpRequestException e)
                {
                    _logger.LogError(e, "Failed to load subscribers for blog {BlogId}", post.BlogId);
                    // The bus must observe failure. Never retry indefinitely inside a consumer.
                    throw;
                }
            } while (true);
        }
    }
}
