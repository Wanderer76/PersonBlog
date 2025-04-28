using Blog.Domain.Entities;
using Blog.Domain.Services.Models.Playlist;
using Shared.Utils;

namespace Blog.Domain.Services;

public interface IPlayListService
{
    Task<Result<PlayListDetailViewModel>> CreatePlayListAsync(PlayListCreateRequest playList);
    Task<Result<PlayListViewModel>> AddVideoToPlayListAsync(PlayListItemAddRequest playList);
    Task<Result<IReadOnlyList<PlayListViewModel>>> GetBlogPlayListsAsync(Guid blogId);
    Task<Result<PlayListDetailViewModel>> GetPlayListDetailAsync(Guid id);
}