using Blog.Contracts.Models;
using Blog.Contracts.Models.Category;
using Blog.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Shared.Models;
using Shared.Utils;
using System.ComponentModel.DataAnnotations;

namespace Blog.Contracts.Services;
public interface IProfilePostV2Service
{
    Task<PagedListViewModel<UserPostInfoDto>> GetCurrentUserPostsAsync(
      Guid blogId,
      int page,
      int pageSize,
      PostType postType);

    Task<CreatePostModelViewModel> GetPostCreateModelAsync();

    Task<Result<UserPostInfoDto>> CreatePostAsync(
        PostCreateCommand command,
        Guid blogId);

    Task<PagedListViewModel<PostCommonModelV2>> GetAvailablePostsByBlogIdAsync(
        Guid currentBlogId,
        Guid requestedBlogId,
        int page,
        int pageSize,
        PostType postType);
}


public class PostCreateRequest
{
    public string Title { get; set; }
    public PostType Type { get; set; }
    public PostVisibility Visibility { get; set; }
    public TextPostCreateForm? TextPostData { get; set; }
    public VideoPostCreateForm? VideoPostData { get; set; }
}

public sealed class VideoPostCreateForm
{
    public IFormFile? Thumbnail { get; set; }
    public string? Description { get; set; }
    public List<int> Categories { get; set; } = [];
}

public sealed class TextPostCreateForm
{
    public string Text { get; set; }
    public IFormFileCollection? Files { get; set; }
}
public class UserPostInfoDto
{
    public Guid Id { get; set; }
    public Guid BlogId { get; set; }
    public int DislikeCount { get; set; }
    public int LikeCount { get; set; }
    public long ViewCount { get; set; }
    public Guid? PaymentSubscriptionId { get; set; }
    public PostVisibility Visibility { get; set; }
    public string Title { get; set; } = default!;

    public TextInfoDto? TextInfo { get; set; }
    public VideoInfoDto? VideoInfo { get; set; }
    public DateTimeOffset CreatedAt { get;  set; }
}
public record CreatePostModelViewModel(
    IEnumerable<SubscriptionLevelModel> SubscriptionLevels,
    IEnumerable<SelectItem<PostVisibility>> Visibility,
    IEnumerable<CategoryModel> CategoryList);


public record PostCreateCommand(
    PostType Type,
    string Title,
    PostVisibility Visibility,
    string? Description,
    string? TextContent,
    IReadOnlyList<int>? CategoryIds,
    IReadOnlyList<PostFileUpload>? TextFiles,
    PostFileUpload? Thumbnail);

// Модель загрузки файла (без IFormFile)
public record PostFileUpload(
    Stream ContentStream,
    string FileName,
    long Length,
    string ContentType);