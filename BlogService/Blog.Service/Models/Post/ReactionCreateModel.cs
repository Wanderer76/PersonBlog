namespace Blog.Service.Models.Post
{
    public class ReactionCreateModel
    {
        public bool? IsLike { get; set; }
        public Guid PostId { get; set; }
        public string RemoteIp { get; set; }
        public Guid? UserId { get; set; }
    }
}