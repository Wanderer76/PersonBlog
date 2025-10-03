using Music.Contract.Models;
using Music.Contract.Models.PlayList;
using Music.Domain.Entities;
using Shared.Utils;

namespace Music.Domain.Services
{
    public interface IMusicPlayListService
    {
        Task<Result<IReadOnlyList<PlayListViewModel>>> GetCurrentUserPlayListsAsync();
        Task<Result<IReadOnlyList<PlayListViewModel>>> CreateDefaultUserPlayListsAsync();
        Task<Result<IReadOnlyList<PlayListViewModel>>> CreateDefaultUserPlayListsAsync(Guid userId);
        Task<Result<PlayListViewModel>> CreatePlayListsAsync(CreatePlayListRequest createPlayList);
        Task<Result<IReadOnlyList<TrackViewItem>>> GetPlayListTrackListAsync(Guid id, int page, int size);
        Task<Result<PlayListViewModel>> GetPlayListInfoAsync(Guid id);
        Task<Result> RemoveTrackFromPlayListAsync(Guid id, Guid trackId);
        Task<Result> AddTrackToPlayListAsync(Guid trackId, ConstPlayListType liked);
        Task<Result> AddTrackToPlayListAsync(Guid id, Guid trackId);
        Task<Result> RemovePlayListAsync(Guid id);
    }
}
