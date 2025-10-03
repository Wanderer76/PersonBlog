using MusicRecommendation.Domain.Domain;

namespace MusicRecommendation.Domain.Repositories
{
    public interface ITrackRepository
    {
        Task<Track> GetByIdAsync(Guid id);
        Task CreateOrUpdate(Track track);
        Task RemoveAsync(Guid id);
    }
}
