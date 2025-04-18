using Shared.Utils;
using ViewReacting.Domain.Entities;
using ViewReacting.Domain.Models;

namespace ViewReacting.Domain.Services;

public interface IViewHistoryService
{
    Task<Result<IReadOnlyList<HistoryViewItem>>> GetUserViewHistoryListAsync(Guid userId);
    Task<Result<bool>> CreateOrUpdateViewHistory(UserPostView postViewer);
}