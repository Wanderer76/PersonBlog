using MessageBus.EventHandler;
using Music.Contract.Events;
using MusicRecommendation.Domain.Domain;
using MusicRecommendation.Domain.Repositories;

namespace MusicRecommendation.Domain.Handlers
{
    public class TrackCreateHandler : IEventHandler<TrackCreateEvent>, IEventHandler<TrackDeleteEvent>
    {
        private readonly ITrackRepository _trackRepository;

        public TrackCreateHandler(ITrackRepository trackRepository)
        {
            _trackRepository = trackRepository;
        }

        public async Task Handle(IMessageContext<TrackCreateEvent> @event)
        {
            var message = @event.Message;
            await _trackRepository.CreateOrUpdate(new Track(message.Id, message.AlbumId, message.ArtistId, message.CreatedAt, [.. message.Genres.Select(x => new TrackGenre(x, message.Id))]));
        }

        public async Task Handle(IMessageContext<TrackDeleteEvent> @event)
        {
            var message = @event.Message;
            await _trackRepository.RemoveAsync(message.Id);
        }
    }
}
