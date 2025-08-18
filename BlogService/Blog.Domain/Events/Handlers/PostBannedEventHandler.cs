using Blog.Contracts.Events;
using Blog.Domain.Entities;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Blog.Domain.Events.Handlers
{
    public class PostBannedEventHandler : IEventHandler<PostBannedEvent>
    {
        private readonly IReadWriteRepository<IBlogEntity> _repository;

        public PostBannedEventHandler(IReadWriteRepository<IBlogEntity> repository)
        {
            _repository = repository;
        }

        public async Task Handle(IMessageContext<PostBannedEvent> @event)
        {
            var post = await _repository.Get<Post>()
                .FirstAsync(x => x.Id == @event.Message.PostId);
            _repository.Attach(post);
            var banMessage = new BanMessage(GuidService.GetNewGuid(), post.Id, @event.Message.CreatedAt, @event.Message.Message);
            _repository.Add(banMessage);
            post.SetPostBanned(banMessage);
            await _repository.SaveChangesAsync();
        }
    }
}
