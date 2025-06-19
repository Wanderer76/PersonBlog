using Blog.Contracts.Events;
using MessageBus.EventHandler;
using Search.Domain.Services;

namespace SearchService.Application.Consumers
{
    public class PostUpdateEventHandler : IEventHandler<PostUpdateEvent>
    {
        private readonly ISearchService _searchService;

        public PostUpdateEventHandler(ISearchService searchService)
        {
            _searchService = searchService;
        }

        public async Task Handle(IMessageContext<PostUpdateEvent> @event)
        {
            if (@event.Message.UpdateType == UpdateType.Create)
            {
                await _searchService.AddPostAsync(new Search.Domain.PostModel
                {
                    BlogId = @event.Message.BlogId,
                    CreatedAt = @event.Message.CreatedAt,
                    Description = @event.Message.Description,
                    Id = @event.Message.PostId,
                    Title = @event.Message.Title,
                    ViewCount = @event.Message.ViewCount
                });
            }
            if (@event.Message.UpdateType == UpdateType.Update)
            {
                await _searchService.UpdatePostAsync(new Search.Domain.PostModel
                {
                    BlogId = @event.Message.BlogId,
                    CreatedAt = @event.Message.CreatedAt,
                    Description = @event.Message.Description,
                    Id = @event.Message.PostId,
                    Title = @event.Message.Title,
                    ViewCount = @event.Message.ViewCount
                });
            }
            if (@event.Message.UpdateType == UpdateType.Delete)
            {
                await _searchService.RemovePostAsync(@event.Message.PostId);
            }
        }
    }
}
