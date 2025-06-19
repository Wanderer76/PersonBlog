using Shared.Services;

namespace Blog.Service.Models
{
    public class VideoCardModel : ICacheKey
    {
        public Guid PostId { get; set; }
        public Guid VideoId { get; set; }
        public string PreviewUrl { get; set; }
        public string Title { get; set; }
        public int ViewCount { get; set; }
        public string BlogName { get; set; }
        public Guid BlogId { get; set; }
        public string BlogLogo { get; set; }

        public string GetKey()
        {
            return $"{nameof(VideoCardModel)}:{PostId}";
        }
    }
}
