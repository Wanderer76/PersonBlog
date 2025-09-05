using MusicRecommendation.Domain.Domain;

namespace MusicRecommendation.Domain.Repositories
{
    public interface IUserListenHistoryRepository
    {
        Task<IReadOnlyList<UserListenHistory>> GetHistoriesByUserIdAsync(Guid userId, int page, int size);
        Task<IReadOnlyList<UserListenHistory>> GetHistoriesByTrackIdAsync(Guid trackId, int page, int size);
        Task CreateHistoryAsync(UserListenHistory history);
        Task<UserListenHistory> GetHistoryByIdAsync(Guid id);
    }
}
