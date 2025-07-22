using Authentication.Contract.Events;
using Comments.Domain.Entities;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Comments.Domain.Services
{
    public class UserCreateEventHandler : IEventHandler<UserCreateEvent>
    {
        private readonly IReadWriteRepository<ICommentEntity> _repository;

        public UserCreateEventHandler(IReadWriteRepository<ICommentEntity> repository)
        {
            _repository = repository;
        }

        public async Task Handle(IMessageContext<UserCreateEvent> @event)
        {
            var message = @event.Message;

            var userProfile = await _repository.Get<UserProfile>()
                .FirstOrDefaultAsync(x => x.UserId == message.UserId);
            if (userProfile != null)
            {
                _repository.Attach(userProfile);
                userProfile.PhotoUrl = message.PhotoUrl;
                userProfile.Username = message.UserName;
            }
            else
            {
                userProfile = new UserProfile(message.UserId, message.UserName, message.PhotoUrl);
                _repository.Add(userProfile);
            }
            await _repository.SaveChangesAsync();
        }
    }
}
