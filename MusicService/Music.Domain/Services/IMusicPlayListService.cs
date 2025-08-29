using Music.Contract.Models.PlayList;
using Shared.Utils;

namespace Music.Domain.Services
{
    public interface IMusicPlayListService
    {
        Task<Result<IReadOnlyList<PlayListViewModel>>> GetCurrentUserPlayListsAsync();
        Task<Result<IReadOnlyList<PlayListViewModel>>> CreateDefaultUserPlayListsAsync();
        Task<Result<IReadOnlyList<PlayListViewModel>>> CreateDefaultUserPlayListsAsync(Guid userId);
        Task<Result<PlayListViewModel>> CreatePlayListsAsync(CreatePlayListRequest createPlayList);
    }
}
