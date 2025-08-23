using Shared.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Profile.Domain.Entities
{
    public class PostBanRequest : IUserEntity
    {
        [Key]
        public Guid Id { get; private set; }
        public long ProfileId { get; private set; }

        public string Message { get; private set; }
        public Guid ReasonId { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        public Guid PostId { get; private set; }

        [ForeignKey(nameof(ProfileId))]
        public AppProfile Profile { get; private set; }

        public PostBanRequest(long profileId, string message, Guid reasonId, Guid postId)
        {
            Id = GuidService.GetNewGuid();
            CreatedAt = DateTimeService.Now();
            ProfileId = profileId;
            Message = message;
            ReasonId = reasonId;
            PostId = postId;
        }
    }
}
