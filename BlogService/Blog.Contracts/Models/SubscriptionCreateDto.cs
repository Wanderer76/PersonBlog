namespace Blog.Contracts.Models
{
    public class SubscriptionCreateDto
    {
        public Guid BlogId { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string Title { get; set; } = null!;
        public Guid? PreviousLevelId { get; set; }
        [System.ComponentModel.DataAnnotations.Range(0, double.MaxValue)]
        public double Price { get; set; }
        public string? Description { get; set; }
        public string? PhotoUrl { get; set; }
    }


    public class SubscriptionUpdateDto : SubscriptionCreateDto
    {
        public Guid Id { get; set; }
    }
}
