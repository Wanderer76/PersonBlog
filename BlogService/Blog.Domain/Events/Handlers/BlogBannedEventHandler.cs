using Blog.Contracts.Events;
using Blog.Domain.Entities;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Blog.Domain.Events.Handlers
{
    public class BlogBannedEventHandler : IEventHandler<BlogBannedEvent>
    {
        private readonly IReadWriteRepository<IBlogEntity> _repository;

        public BlogBannedEventHandler(IReadWriteRepository<IBlogEntity> repository)
        {
            _repository = repository;
        }

        public async Task Handle(IMessageContext<BlogBannedEvent> @event)
        {
            var blog = await _repository.Get<PersonBlog>()
                .FirstAsync(x=>x.Id == @event.Message.BlogId);
            _repository.Attach(blog);
            
        }
    }
}
