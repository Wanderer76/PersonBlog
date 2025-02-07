namespace Profile.Service.Models.Post
{
    public class ViewedVideoModel
    {
        public Guid PostId { get; }
        public Guid? UserId { get; }
        public string? RemoteIp { get; }

        public ViewedVideoModel(Guid postId, Guid? userId, string? remoteIp)
        {
            PostId = postId;
            UserId = userId;
            RemoteIp = remoteIp;
        }
    }

}
