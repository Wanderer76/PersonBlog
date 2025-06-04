using MessageBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blog.Contracts.Events
{
    [EventPublish(Exchange = "view-reacting",RoutingKey = "video.sync")]
    public class UserViewedSyncEvent
    {
        public Guid EventId { get; set; }
        public Guid? UserId { get; set; }
        public Guid PostId { get; set; }
        public bool IsViewed { get; set; }
        public string? RemoteIp { get; set; }
        public DateTimeOffset WatchedTime { get; set; }
    }

    [EventPublish(Exchange = "view-reacting", RoutingKey = "video.sync")]
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
