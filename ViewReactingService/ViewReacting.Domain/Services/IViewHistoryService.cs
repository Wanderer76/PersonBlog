using Shared.Utils;
using ViewReacting.Domain.Entities;
using ViewReacting.Domain.Models;

namespace ViewReacting.Domain.Services;

public interface IViewHistoryService
{
    Task<Result<IReadOnlyList<HistoryViewItem>>> GetUserViewHistoryListAsync(Guid userId);
    Task<Result<HistoryViewItem>> GetUserViewHistoryItemAsync(Guid postId, Guid userId);
    Task<Result<UpdateViewState>> CreateOrUpdateViewHistory(UserPostView postViewer);
}

public enum UpdateViewState
{
    Created,
    Updated,
}