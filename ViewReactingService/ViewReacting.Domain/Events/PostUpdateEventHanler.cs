using Blog.Contracts.Events;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using ViewReacting.Domain.Entities;

namespace ViewReacting.Domain.Events
{
    public class PostUpdateEventHandler : IEventHandler<PostUpdateEvent>
    {
        private readonly IReadWriteRepository<IUserEntity> _repository;

        public PostUpdateEventHandler(IReadWriteRepository<IUserEntity> repository)
        {
            _repository = repository;
        }

        public async Task Handle(IMessageContext<PostUpdateEvent> @event)
        {
            var message = @event.Message;
            if (message.UpdateType == UpdateType.Delete)
            {
                var postReactions = await _repository.Get<PostReaction>()
                    .Where(x => x.PostId == message.PostId)
                    .ExecuteUpdateAsync(x => x.SetProperty(x => x.IsDelete, x => true));

                var videoView = await _repository.Get<UserPostView>()
                    .Where(x => x.PostId == message.PostId)
                    .ExecuteUpdateAsync(x => x.SetProperty(x => x.IsDelete, x => true));
            }

            if(message.UpdateType == UpdateType.Update)
            {
                var postReactions = await _repository.Get<PostReaction>()
                    .Where(x => x.PostId == message.PostId)
                    .ExecuteUpdateAsync(x => x.SetProperty(x => x.IsDelete, x => false));

                var videoView = await _repository.Get<UserPostView>()
                    .Where(x => x.PostId == message.PostId)
                    .ExecuteUpdateAsync(x => x.SetProperty(x => x.IsDelete, x => false));
            }
        }
    }
}
