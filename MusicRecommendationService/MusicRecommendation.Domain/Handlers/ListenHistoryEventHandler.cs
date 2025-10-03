using MessageBus.EventHandler;
using Music.Contract.Events;
using MusicRecommendation.Domain.Repositories;

namespace MusicRecommendation.Domain.Handlers
{
    public class ListenHistoryEventHandler : IEventHandler<ListenHistoryEvent>
    {
        private readonly IUserListenHistoryRepository _userListenHistoryRepository;

        public ListenHistoryEventHandler(IUserListenHistoryRepository userListenHistoryRepository)
        {
            _userListenHistoryRepository = userListenHistoryRepository;
        }

        public async Task Handle(IMessageContext<ListenHistoryEvent> @event)
        {
            var message = @event.Message;
            await _userListenHistoryRepository.CreateHistoryAsync(new Domain.UserListenHistory(message.Id, message.UserId, message.ListenedAt, message.TrackId));
        }
    }
}
