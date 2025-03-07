using System.ComponentModel.DataAnnotations;

namespace MessageBus.Shared.Events
{
    public class UserViewedPostEvent
    {
        public Guid EventId { get; set; }
        public Guid? UserId { get; set; }
        public Guid PostId { get; set; }
        public bool IsViewed {  get; set; }
        public string? RemoteIp { get; set; }
        public DateTimeOffset ViewedAt { get; set; }

        public bool? IsLike { get; set; }

        public UserViewedPostEvent()
        {

        }

        public UserViewedPostEvent(Guid id, Guid? userId, Guid postId, DateTimeOffset viewedAt, string? remoteIp)
        {
            EventId = id;
            UserId = userId;
            PostId = postId;
            ViewedAt = viewedAt;
            RemoteIp = remoteIp;
        }

        public UserViewedPostEvent(Guid id, Guid? userId, Guid postId, DateTimeOffset viewedAt, string? remoteIp, bool? isLike,bool isViewed)
            : this(id, userId, postId, viewedAt, remoteIp)
        {
            IsLike = isLike;
            IsViewed = isViewed;
        }
    }

    public class UserViewedSyncEvent : UserViewedPostEvent
    {

    }
}
