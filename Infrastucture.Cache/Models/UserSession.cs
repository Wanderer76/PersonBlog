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
        public bool IsViewed { get; set; }
        public bool? IsLike { get; set; }
        public bool IsSubscribe { get; set; }

        public PostView()
        {

        }

        public PostView(bool isViewed, bool? isLike, bool isSubscribe)
        {
            IsViewed = isViewed;
            IsLike = isLike;
            IsSubscribe = isSubscribe;
        }

    }
}
