using System.ComponentModel.DataAnnotations;

namespace MessageBus.Shared.Events
{
    public class UserReactionPostEvent
    {
        public Guid EventId { get; set; }
        public Guid? UserId { get; set; }
        public Guid PostId { get; set; }
        public bool IsViewed { get; set; }
        public string? RemoteIp { get; set; }
        public double ReactionTime { get; set; }

        public bool? IsLike { get; set; }

        public UserReactionPostEvent()
        {

        }

        public UserReactionPostEvent(Guid id, Guid? userId, Guid postId, double reactionTime, string? remoteIp)
        {
            EventId = id;
            UserId = userId;
            PostId = postId;
            ReactionTime = reactionTime;
            RemoteIp = remoteIp;
        }

        public UserReactionPostEvent(Guid id, Guid? userId, Guid postId, double reactionTime, string? remoteIp, bool? isLike, bool isViewed)
            : this(id, userId, postId, reactionTime, remoteIp)
        {
            IsLike = isLike;
            IsViewed = isViewed;
        }
    }

    public class UserViewedSyncEvent
    {
        public Guid EventId { get; set; }
        public Guid? UserId { get; set; }
        public Guid PostId { get; set; }
        public bool IsViewed { get; set; }
        public string? RemoteIp { get; set; }
        public DateTimeOffset WatchedTime { get; set; }
    }
 
    public class UserReactionSyncEvent
    {
        public Guid EventId { get; set; }
        public Guid? UserId { get; set; }
        public Guid PostId { get; set; }
        public string? RemoteIp { get; set; }
        public double Time { get; set; }
        public bool? IsLike { get; set; }
    }
}
