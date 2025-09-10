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
        var skip = (page - 1) * count;

        var recommendations = await _context.Tracks
            .AsNoTracking()
            .AsSplitQuery()
            .Where(t => !_context.UserListenHistory
                .Where(h => h.UserId == userId)
                .Select(h => h.TrackId)
                .Contains(t.Id)) // Не слушал этот трек
            .Select(t => new
            {
                TrackId = t.Id,
                ArtistMatch = _context.Tracks
                    .Where(tr => _context.UserListenHistory
                        .Where(h => h.UserId == userId)
                        .Select(h => h.TrackId)
                        .Contains(tr.Id))
                    .Select(tr => tr.ArtistId)
                    .Distinct()
                    .Contains(t.ArtistId) ? 1 : 0,

                GenreMatchCount = _context.TrackGenres
                        .Where(tg => _context.UserListenHistory
                        .Where(h => h.UserId == userId)
                        .Select(h => h.TrackId)
                        .Contains(tg.TrackId))
                    .Select(tg => tg.Id)
                    .Intersect(_context.TrackGenres
                        .Where(tg2 => tg2.TrackId == t.Id)
                        .Select(tg2 => tg2.Id))
                    .Count()
            })
            .Select(t => new
            {
                t.TrackId,
                Score = (t.GenreMatchCount * 2) + t.ArtistMatch,
                ListenCount = _context.UserListenHistory
                    .Where(h => h.TrackId == t.TrackId)
                    .Count()
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.ListenCount)
            .Skip(skip)
            .Take(count)
            .Select(x => x.TrackId)
            .ToListAsync();

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
