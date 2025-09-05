using Microsoft.EntityFrameworkCore;
using MusicRecommendation.Domain.Domain;
using MusicRecommendation.Domain.Repositories;

namespace MusicRecommendation.Persistence.Repositories
{
    internal class TrackRepository : ITrackRepository
    {
        private readonly MusicRecommendationDbContext _context;

        public TrackRepository(MusicRecommendationDbContext context)
        {
            _context = context;
        }

        public async Task CreateOrUpdate(Track track)
        {
            var exist = await _context.Tracks.FirstOrDefaultAsync(x => x.Id == track.Id);
            if (exist != null)
            {

            }
            else
            {
                _context.Tracks.Add(track);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<Track> GetByIdAsync(Guid id)
        {
            return await _context.Tracks.FirstAsync(x => x.Id == id);
        }

        public async Task RemoveAsync(Guid id)
        {
            var exist = await _context.Tracks.FirstOrDefaultAsync(x => x.Id == id);
            if (exist != null)
            {
                _context.Tracks.Remove(exist);
            }
            await _context.SaveChangesAsync();
        }
    }
}
