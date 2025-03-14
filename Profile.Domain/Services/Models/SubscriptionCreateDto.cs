namespace Blog.Domain.Services.Models
{
    public class SubscriptionCreateDto
    {
        public Guid BlogId { get; set; }
        public string Title { get; set; }
        public Guid? PreviousLevelId { get; set; }
        public double Price { get; set; }
        public string? Description { get; set; }
        public string? PhotoUrl { get; set; }
    }


    public class SubscriptionUpdateDto : SubscriptionCreateDto
    {
        public Guid Id { get; set; }
    }
}
