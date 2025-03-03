using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Cache.Models
{
    public class UserSession
    {
        public Guid? UserId { get; set; }
        public string? IpAddress { get; set; }

        public bool IsAnonymous => UserId != null;

        public List<PostView> PostViews { get; set; } = new List<PostView>();
    }
    public class PostView
    {
        public Guid PostId { get; set; }
        public bool IsViewed { get; set; }
        public bool? IsLike { get; set; }
        public bool IsSubscribe { get; set; }

        public PostView()
        {

        }

        public PostView(Guid postId, bool isViewed, bool? isLike, bool isSubscribe)
        {
            PostId = postId;
            IsViewed = isViewed;
            IsLike = isLike;
            IsSubscribe = isSubscribe;
        }

    }
}
