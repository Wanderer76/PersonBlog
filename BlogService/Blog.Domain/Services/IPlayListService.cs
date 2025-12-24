using Blog.Contracts.Models;
using Blog.Domain.Entities;
using Blog.Domain.Services.Models;
using Blog.Domain.Services.Models.Playlist;
using Shared.Utils;

namespace Blog.Domain.Services;

[Obsolete]
public interface IPlayListService
{
    Task<Result<PlayListDetailViewModel>> CreatePlayListAsync(PlayListCreateRequest playList);
    Task<Result<PlayListDetailViewModel>> UpdatePlayListCommonDataAsync(PlayListUpdateRequest playList);
    Task<Result<PlayListViewModel>> AddVideoToPlayListAsync(PlayListItemAddRequest playList);
    Task<Result<PlayListViewModel>> RemoveVideoFromPlayListAsync(PlayListItemRemoveRequest playList);
    Task<Result<IReadOnlyList<PlayListViewModel>>> GetBlogPlayListsAsync(Guid blogId);
    Task<Result<PlayListDetailViewModel>> GetPlayListDetailAsync(Guid id);
    Task<Result<IEnumerable<PostCommonModel>>> GetAvailablePostsToPlayListByIdAsync(IEnumerable<Guid> playlistId);
    Task<Result<PlayListViewModel>> ChangePostPositionAsync(ChangePostPositionRequest changePostPositionRequest);
    Task<Result<bool>> RemovePlayListAsync(Guid id);
}