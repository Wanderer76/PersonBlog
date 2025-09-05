using MusicRecommendation.Domain.Domain;
using Shared.Utils;

namespace MusicRecommendation.Domain.Repositories
{
    public interface ITrackRecommendationRepository
    {
        Task<IReadOnlyList<UserTrackAudition>> GetUserPopularTracksAsync(Guid userId, int page, int size);
        Task<List<Guid>> GetContentBasedRecommendations(Guid userId, int page = 1, int count = 10);
        Task<UserTrackAudition?> GetUserTrackAudition(Guid userId, Guid trackId);
        Task<Result> UpdateAsync(UserTrackAudition userTrackAudition);
    }
}
