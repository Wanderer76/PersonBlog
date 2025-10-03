using Infrastructure.Interface;
using Shared.Services;
using Shared.Utils;
using System.ComponentModel.DataAnnotations;

namespace Music.Domain.Entities
{
    public class Artist : IMusicEntity, ISoftDelete
    {
        [Key]
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public Guid? UserId { get; private set; }
        public bool IsExternal { get; private set; }
        public Guid? AvatarId { get; private set; }
        public List<ArtistTrackLink> ArtistTrackLinks { get; private set; } = [];

        public bool IsDelete { get; private set; }
        public DateTimeOffset? DeleteDateTime { get; private set; }

        public AvatarMetadata AvatarMetadata { get; private set; }

        public static Artist CreateForUser(string name, Guid userId, Guid? avatarUrl)
        {
            return new Artist
            {
                Id = GuidService.GetNewGuid(),
                Name = name.Trim(),
                UserId = userId,
                IsExternal = false,
                AvatarId = avatarUrl,
                ArtistTrackLinks = new List<ArtistTrackLink>()
            };
        }

        public static Artist CreateExternal(string name, Guid? avatarUrl)
        {
            return new Artist
            {
                Id = GuidService.GetNewGuid(),
                Name = name.Trim(),
                UserId = null,
                IsExternal = true,
                AvatarId = avatarUrl,
                ArtistTrackLinks = new List<ArtistTrackLink>()
            };
        }

        public Result AddTrack(Guid trackId, bool isMain = true)
        {
            ArtistTrackLinks.Add(new ArtistTrackLink(trackId, Id, isMain));
            return Result.Success();
        }

        public Result DeleteArtist()
        {
            IsDelete = true;
            DeleteDateTime = DateTimeService.Now();
            return Result.Success();
        }
    }
}
