using Blog.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Blog.Service.Models.Post
{
    public class PostEditDto
    {
        public Guid Id { get; }
        public Guid UserId { get; }
        public string? Description { get; }
        public string Title { get; }
        public IFormFile? PreviewId { get; }
        public List<int> Categories { get; }

        public PostEditDto(Guid id, Guid userId, string? description, string title, IFormFile? previewId, List<int> categories)
        {
            Id = id;
            UserId = userId;
            Description = description;
            Title = title;
            PreviewId = previewId;
            Categories = categories;
        }
    }

    public class PostEditViewModel
    {
        public Guid Id { get; }
        public string Title { get; }
        public string? Description { get; }
        public string? PreviewId { get; }
        public PostVisibility Visibility { get; }
        public Guid? PaymentSubscriptionId { get; }
        public List<int> Categories { get; }

        public PostEditViewModel(Guid id, string title, string? description, string? previewId, PostVisibility visibility, Guid? paymentSubscriptionId, List<int> categories)
        {
            Id = id;
            Title = title;
            Description = description;
            PreviewId = previewId;
            Visibility = visibility;
            PaymentSubscriptionId = paymentSubscriptionId;
            Categories = categories;
        }
    }
}
