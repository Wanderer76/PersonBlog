using Shared.Utils;
using System.ComponentModel.DataAnnotations.Schema;

namespace Music.Domain.Entities
{
    public class Track : IMusicEntity
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public Guid? AlbumId { get; private set; }
        public Guid? PostId { get; private set; }
        public string ArtistName { get; private set; }
        public string ThumbnailUrl { get; private set; }

        public Guid TrackFileId { get; private set; }

        [ForeignKey(nameof(TrackFileId))]
        public TrackMetadata Metadata { get; private set; }

        public List<ArtistTrackLink> ArtistTrackLinks { get; private set; }


        public Result AddArtist(Guid artistId, bool isMain = true)
        {
            ArtistTrackLinks.Add(new ArtistTrackLink(Id, artistId, isMain));
            return Result.Success();
        }
    }
}
