using Authentication.Contract.Events;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Music.Domain.Entities;
using Shared.Persistence;

namespace Music.Domain.EventHandlers
{
    internal class ProfileRegisterEventHandler : IEventHandler<ProfileRegisterEvent>
    {

        private readonly IReadWriteRepository<IMusicEntity> _repository;

        public ProfileRegisterEventHandler(IReadWriteRepository<IMusicEntity> repository)
        {
            _repository = repository;
        }

        public async Task Handle(IMessageContext<ProfileRegisterEvent> @event)
        {
            var message = @event.Message;
            if (!await _repository.Get<AppProfile>().AnyAsync(x => x.UserId == message.UserId))
            {
                _repository.Add(new AppProfile(message.UserId, message.UserId, message.Name, true));
                _repository.Add(new PlayList(PlayListConstants.UploadPlaylistName, message.UserId, ConstPlayListType.Upload, []));
                _repository.Add(new PlayList(PlayListConstants.LikedTracksPlayList, message.UserId, ConstPlayListType.Liked, []));
                await _repository.SaveChangesAsync();
            }
        }
    }
}
