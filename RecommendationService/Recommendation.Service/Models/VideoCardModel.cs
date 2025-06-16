namespace Blog.Service.Models
{
    public class VideoCardModel
    {
        public Guid PostId { get; set; }
        public Guid VideoId { get; set; }
        public string PreviewUrl { get; set; }
        public string Title { get; set; }
        public int ViewCount { get; set; }
        public string BlogName { get; set; }
        public Guid BlogId { get; set; }
        public string BlogLogo { get; set; }
    }
}
