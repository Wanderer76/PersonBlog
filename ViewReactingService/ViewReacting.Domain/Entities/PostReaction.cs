using Shared.Services;

namespace ViewReacting.Domain.Entities
{
    public class PostReaction : IVideoReactEntity
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; private set; }
        public string IpAddress { get; private set; }
        public Guid PostId { get; set; }
        public double ReactionTime { get; set; }
        public bool IsLike { get; set; }
        public bool IsDelete { get; set; }

        public PostReaction(Guid? userId, string ipAddress, Guid postId, double reactionTime, bool isLike)
        {
            Id = GuidService.GetNewGuid();
            UserId = userId;
            IpAddress = ipAddress;
            PostId = postId;
            ReactionTime = reactionTime;
            IsLike = isLike;
            IsDelete = false;
        }
    }
}
