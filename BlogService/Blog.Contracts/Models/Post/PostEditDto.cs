using Blog.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Blog.Contracts.Models.Post;

public class PostEditDto
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    [MinLength(3)]
    public string Title { get; set; }
    public IFormFile? Preview { get; set; }
    public List<int>? Categories { get; set; }
    public PostVisibility Visibility { get; set; }
}

public class PostEditViewModel
{
    public Guid Id { get; }
    public string Title { get; }
    public string? Description { get; }
    public string? PreviewUrl { get; }
    public PostVisibility Visibility { get; }
    public Guid? PaymentSubscriptionId { get; }
    public List<int> Categories { get; }

    public PostEditViewModel(Guid id, string title, string? description, string? previewUrl, PostVisibility visibility, Guid? paymentSubscriptionId, List<int> categories)
    {
        Id = id;
        Title = title;
        Description = description;
        PreviewUrl = previewUrl;
        Visibility = visibility;
        PaymentSubscriptionId = paymentSubscriptionId;
        Categories = categories;
    }
}
