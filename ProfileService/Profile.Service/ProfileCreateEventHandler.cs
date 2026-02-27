using Authentication.Contract.Events;
using MessageBus.EventHandler;
using Profile.Domain.Entities;
using Shared.Persistence;

namespace Profile.Domain.Events
{
    public class ProfileCreateEventHandler : IEventHandler<ProfileRegisterEvent>
    {
        private readonly IWriteRepository<IUserEntity> _repository;

        public ProfileCreateEventHandler(IWriteRepository<IUserEntity> repository)
        {
            _repository = repository;
        }

        public async Task Handle(IMessageContext<ProfileRegisterEvent> @event)
        {
            var message = @event.Message;
            _repository.Add(AppProfile.Create(message.Name, message.UserId));
            await _repository.SaveChangesAsync();
        }
    }
}
