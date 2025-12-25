using Blog.Contracts.Models;
using PlayListService.Services.Models;
using Shared.Models;
using Shared.Utils;

namespace PlayListService.Services.Services;
public interface IPlayListService
{
    Task<Result<PlayListListItem>> AddVideoAsync(PlayListItemAddRequest playListItems);
    Task<Result<PlayListListItem>> ChangePostPositionAsync(ChangePostPositionRequest changePostPositionRequest);
    Task<Result<PlayListListItem>> CreatePlayListAsync(CreatePlayListRequest request);
    Task<Result<PlayListListItem>> GetPlayListAsync(Guid id);
    Task<PagedListViewModel<PostCommonModel>> GetPlayListPostPagedAsync(Guid playListId, int page, int pageSize);
    Task<Result<IReadOnlyList<PlayListListItem>>> GetPlayListsByBlogIdAsync(Guid blogId);
    Task<IReadOnlyList<PlayListListItem>> GetUserPlayLists();
    Task<Result> RemovePlayListAsync(Guid id);
    Task<Result<PlayListListItem>> RemoveVideoAsync(PlayListItemRemoveRequest request);
}