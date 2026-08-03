using Blog.Contracts.Models;
using Blog.Contracts.Models.Category;
using Blog.Contracts.Models.Post;
using Blog.Domain.Entities;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Shared.Models;
using Shared.Utils;
using System.ComponentModel.DataAnnotations;

namespace Blog.Contracts.Services;
public interface IProfilePostV2Service
{
    Task<PagedListViewModel<UserPostInfoModel>> GetCurrentUserPostsAsync(Guid blogId, int page, int pageSize, PostType postType);
    Task<CreatePostModelViewModel> GetPostCreateModelAsync();
    Task<Result<UserPostInfoModel>> CreatePostAsync(PostCreateCommand command);
    Task<PagedListViewModel<PostCommonModelV2>> GetAvailablePostsByBlogIdAsync(Guid requestedBlogId, int page, int pageSize, PostType postType);
    Task<Result> RemovePostAsync(Guid postId);
    Task<Result<PostEditViewModel>> GetPostEditViewModelAsync(Guid postId);

    Task<Result> UpdatePostAsync(PostUpdateRequest updateRequest);

}

public class VideoPostCreateRequest
{
    public string Title { get; set; } = null!;
    public PostVisibility Visibility { get; set; }
    public VideoPostCreateForm VideoPostData { get; set; } = null!;
}

public sealed class PostUpdateRequest
{
    public Guid Id { get; }
    public string? Description { get; }
    public string Title { get; }
    public FileMetadataModel? Preview { get; }
    public List<int> Categories { get; }
    public PostVisibility Visibility { get; }

    public PostUpdateRequest(
        Guid id,
        string? description,
        string title,
        FileMetadataModel? previewId,
        List<int> categories,
        PostVisibility visibility)
    {
        Id = id;
        Description = description?.Trim();
        Title = title.Trim();
        Preview = previewId;
        Categories = categories;
        Visibility = visibility;
    }
}

public sealed class VideoPostCreateForm
{
    public IFormFile? Thumbnail { get; set; }
    public string? Description { get; set; }
    public List<int> Categories { get; set; } = [];
}

public sealed class TextPostCreateForm
{
    [Required]
    public string Title { get; set; } = null!;
    public string? Text { get; set; }

    public PostVisibility Visibility { get; set; }
    public IFormFileCollection? Media { get; set; }
}

public class UserPostInfoModel
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
    public DateTimeOffset CreatedAt { get; set; }
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
    IReadOnlyList<FileMetadataModel>? TextFiles,
    FileMetadataModel? Thumbnail);

public record PostFileUpload(Stream ContentStream, string FileName, long Length, string ContentType);
