using Microsoft.EntityFrameworkCore;
using MusicRecommendation.Domain.Domain;
using MusicRecommendation.Domain.Repositories;
using Shared.Persistence;
using Shared.Utils;

namespace MusicRecommendation.Persistence.Repositories;

internal class TrackRecommendationRepository : ITrackRecommendationRepository
{
    private readonly MusicRecommendationDbContext _context;
    private readonly IReadWriteRepository<IRecommendation> _repository;

    public TrackRecommendationRepository(MusicRecommendationDbContext context, IReadWriteRepository<IRecommendation> repository)
    {
        _context = context;
        _repository = repository;
    }

    public async Task<IReadOnlyList<UserTrackAudition>> GetUserPopularTracksAsync(Guid userId, int page, int size)
    {
        return await _context.UserTrackAuditions
             .Where(x => x.UserId == userId)
             .OrderByDescending(x => x.UpdateDate)
             .ThenByDescending(x => x.Count)
             .Skip((page - 1) * size)
             .Take(size)
             .ToListAsync();
    }

    public async Task<List<Guid>> GetContentBasedRecommendations(Guid userId, int page = 1, int count = 10)
    {
        // Сначала получим все необходимые данные отдельными запросами
        var userListenedTracks = await _context.UserListenHistory
            .Where(e => e.UserId == userId)
            .Select(e => e.TrackId)
            .Distinct()
            .ToListAsync();

        var userGenres = await _context.TrackGenres
            .Where(t => userListenedTracks.Contains(t.TrackId))
            .ToListAsync();

        var userArtists = await _context.Tracks
            .Where(t => userListenedTracks.Contains(t.Id))
            .Select(t => t.ArtistId)
            .Distinct()
            .ToListAsync();

        var trackListenCounts = await _context.UserListenHistory
            .GroupBy(e => e.TrackId)
            .Select(g => new { TrackId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TrackId, x => x.Count);

        // Теперь получим все треки для рекомендаций
        var allTracks = await _context.Tracks
            .Where(t => !userListenedTracks.Contains(t.Id))
            .Select(t => new { t.Id, t.Genres, t.ArtistId })
            .ToListAsync();

        // Вычисляем score в памяти
        var recommendations = allTracks
            .Select(t => new
            {
                TrackId = t.Id,
                Score = (t.Genres.Intersect(userGenres).Count() * 2) +
                       (userArtists.Contains(t.ArtistId) ? 1 : 0),
                ListenCount = trackListenCounts.TryGetValue(t.Id, out var count) ? count : 0
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.ListenCount)
            .Skip((page - 1) * count)
            .Take(count)
            .Select(x => x.TrackId)
            .ToList();

        return recommendations;
    }

    public Task<UserTrackAudition?> GetUserTrackAudition(Guid userId, Guid trackId)
    {
        return _context.UserTrackAuditions
            .Where(x => x.UserId == userId)
            .Where(x => x.TrackId == trackId)
            .FirstOrDefaultAsync();
    }

    public async Task<Result> UpdateAsync(UserTrackAudition userTrackAudition)
    {
        var userId = userTrackAudition.UserId;
        var trackId = userTrackAudition.TrackId;

        var audition = await _context.UserTrackAuditions
            .Where(x => x.UserId == userId)
            .Where(x => x.TrackId == trackId)
            .FirstOrDefaultAsync();
        if (audition == null)
        {
            return Result.Failure(new Error("Not found"));
        }
        audition.UpdateCount(userTrackAudition.Count);
        await _context.SaveChangesAsync();
        return Result.Success();
    }
}
