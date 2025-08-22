using Shared.Services;
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

        public DateTimeOffset CreatedAt { get; private set; }

        [ForeignKey(nameof(TrackFileId))]
        public TrackMetadata Metadata { get; private set; }

        public List<ArtistTrackLink> ArtistTrackLinks { get; private set; }

        public Track(string title, Guid? albumId, Guid? postId, string artistName, string thumbnailUrl, Guid trackFileId, List<ArtistTrackLink> artistTrackLinks)
        {
            Id = GuidService.GetNewGuid();
            Title = title;
            AlbumId = albumId;
            PostId = postId;
            ArtistName = artistName;
            ThumbnailUrl = thumbnailUrl;
            TrackFileId = trackFileId;
            ArtistTrackLinks = artistTrackLinks;
            CreatedAt = DateTimeService.Now();
        }

        public Result AddArtist(Guid artistId, bool isMain = true)
        {
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

        public Result UpdateArtistName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure(new("name is empty"));
            }
            Title = ArtistName.Trim();
            return Result.Success();
        }
    }
}