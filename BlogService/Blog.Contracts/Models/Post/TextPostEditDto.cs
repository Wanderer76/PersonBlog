using Blog.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Blog.Contracts.Models.Post;

public sealed class TextPostEditDto
{
    public Guid Id { get; set; }

    [Required, MinLength(3)]
    public string Title { get; set; } = null!;

    public string? Text { get; set; }

    public PostVisibility Visibility { get; set; }

    public IFormFileCollection? Media { get; set; }

    public List<Guid> RemovedMediaIds { get; set; } = [];
}

public sealed record TextPostEditViewModel(
    Guid Id,
    string Title,
    string? Text,
    PostVisibility Visibility,
    IReadOnlyList<TextPostMediaViewModel> Media);

public sealed record TextPostMediaViewModel(
    Guid Id,
    string Name,
    string Url,
    string ContentType,
    long Length);
