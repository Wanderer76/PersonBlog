using Blog.Contracts.Events;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Shared.Persistence;

namespace Profile.Domain.Events
{
    public class BlogCreateEventHandler : IEventHandler<BlogCreateEvent>
    {
        private readonly IReadWriteRepository<IUserEntity> _repository;

        public BlogCreateEventHandler(IReadWriteRepository<IUserEntity> repository)
        {
            _repository = repository;
        }

        public async Task Handle(IMessageContext<BlogCreateEvent> @event)
        {
            var message = @event.Message;

            var user = await _repository.Get<AppProfile>()
                .Where(x => x.UserId == message.UserId)
                .FirstOrDefaultAsync();

            if (user != null && user.BlogId != message.BlogId)
            {
                _repository.Attach(user);
                user.BlogId = message.BlogId;
                await _repository.SaveChangesAsync();
            }
        }
    }
}
