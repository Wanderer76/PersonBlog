using Shared.Services;
using Shared.Utils;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Music.Domain.Entities
{
    public class Track : IMusicEntity
    {
        [Key]
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public Guid? AlbumId { get; private set; }
        public Guid? PostId { get; private set; }
        public Guid UploadedByUserId { get; private set; }
        public Guid? ThumbnailId { get; private set; }
        public Guid TrackFileId { get; private set; }

        public short Year {  get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        [ForeignKey(nameof(TrackFileId))]
        public TrackMetadata Metadata { get; private set; }

        [ForeignKey(nameof(ThumbnailId))]
        public ThumbnailMetadata? ThumbnailMetadata { get; private set; }

        public List<ArtistTrackLink> ArtistTrackLinks { get; private set; }

        private Track()
        {
            ArtistTrackLinks = new List<ArtistTrackLink>();
        }

        public Track(string title, Guid uploadedByUserId, Guid? albumId, Guid? postId, Guid? thumbnailId, Guid trackFileId, short year)
        {
            Id = GuidService.GetNewGuid();
            Title = title;
            AlbumId = albumId;
            PostId = postId;
            ThumbnailId = thumbnailId;
            TrackFileId = trackFileId;
            ArtistTrackLinks = [];
            CreatedAt = DateTimeService.Now();
            UploadedByUserId = uploadedByUserId;
            Year = year;
        }

        public Result AddArtist(Guid artistId, bool isMain = true)
        {
            if (!ArtistTrackLinks.Any(x => x.ArtistId == artistId))
                ArtistTrackLinks.Add(new ArtistTrackLink(Id, artistId, isMain));
            return Result.Success();
        }

        public Result UpdateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return Result.Failure(new("title is empty"));
            }
            Title = title.Trim();
            return Result.Success();
        }
    }
}