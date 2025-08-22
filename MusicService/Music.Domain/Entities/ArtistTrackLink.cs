using Shared.Services;
using System.ComponentModel.DataAnnotations.Schema;

namespace Music.Domain.Entities
{
    public class ArtistTrackLink : IMusicEntity
    {
        public Guid TrackId { get; private set; }
        public Guid ArtistId { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public bool IsMain {  get; private set; }

        [ForeignKey(nameof(TrackId))]
        public Track Track { get; private set; }

        [ForeignKey(nameof(ArtistId))]
        public Artist Artist { get; private set; }

        public ArtistTrackLink(Guid trackId, Guid artistId, bool isMain)
        {
            TrackId = trackId;
            ArtistId = artistId;
            CreatedAt = DateTimeService.Now();
            IsMain = isMain;
        }
    }
}
