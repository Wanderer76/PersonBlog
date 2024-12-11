namespace Profile.Service.Models.Blog
{
    public class BlogCreateDto
    {
        public Guid UserId { get; }
        public string Title { get; }
        public string? Description { get; }
        public string? PhotoUrl { get; }

        public BlogCreateDto(Guid userId, string title, string? description, string? photoUrl)
        {
            UserId = userId;
            Title = title;
            Description = description;
            PhotoUrl = photoUrl;
        }
    }
}
