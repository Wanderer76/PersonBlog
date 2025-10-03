using Microsoft.EntityFrameworkCore;
using MusicRecommendation.Domain.Domain;
using MusicRecommendation.Domain.Repositories;

namespace MusicRecommendation.Persistence.Repositories
{
    internal class UserListenHistoryRepository : IUserListenHistoryRepository
    {
        private readonly MusicRecommendationDbContext _context;

        public UserListenHistoryRepository(MusicRecommendationDbContext context)
        {
            _context = context;
        }

        public async Task CreateHistoryAsync(UserListenHistory history)
        {
            _context.UserListenHistory.Add(history);
            var audition = await _context.UserTrackAuditions.FirstOrDefaultAsync(x => x.TrackId == history.TrackId && x.UserId == history.UserId);
            if (audition == null)
            {
                _context.UserTrackAuditions.Add(new UserTrackAudition(history.UserId, history.TrackId, 1));
            }
            else
            {
                audition.UpdateCount(audition.Count + 1);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<UserListenHistory>> GetHistoriesByTrackIdAsync(Guid trackId, int page, int size)
        {
            return await _context.UserListenHistory
                .AsNoTracking()
                .Where(x => x.TrackId == trackId)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<UserListenHistory>> GetHistoriesByUserIdAsync(Guid userId, int page, int size)
        {
            return await _context.UserListenHistory
                          .AsNoTracking()
                          .Where(x => x.UserId == userId)
                          .Skip((page - 1) * size)
                          .Take(size)
                          .ToListAsync();
        }

        public async Task<UserListenHistory> GetHistoryByIdAsync(Guid id)
        {

            return await _context.UserListenHistory
                          .AsNoTracking()
                          .Where(x => x.Id == id)
                          .FirstAsync();
        }
    }
}
