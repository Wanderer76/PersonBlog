using Infrastructure.Interface;
using Shared.Services;
using Shared.Utils;

namespace Music.Domain.Entities
{
    public class Artist : IMusicEntity, ISoftDelete
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public Guid UserId { get; private set; }
        public string AavatarUrl { get; private set; }
        public List<ArtistTrackLink> ArtistTrackLinks { get; private set; } = [];

        public bool IsDelete { get; private set; }
        public DateTimeOffset? DeleteDateTime { get; private set; }

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
