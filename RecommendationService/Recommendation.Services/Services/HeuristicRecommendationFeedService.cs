using System.Buffers.Binary;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Recommendation.Domain.Entities;
using Recommendation.Domain.Enums;
using Recommendation.Services.Abstractions;
using Recommendation.Services.Models;
using Recommendation.Services.Options;

namespace Recommendation.Services.Services;

public sealed class HeuristicRecommendationFeedService : IRecommendationFeedService
{
    private readonly IRecommendationFeedStore _store;
    private readonly IClock _clock;
    private readonly RecommendationFeedOptions _options;
    private readonly RecommendationCursorCodec _cursorCodec;

    public HeuristicRecommendationFeedService(
        IRecommendationFeedStore store,
        IClock clock,
        IOptions<RecommendationFeedOptions> options)
    {
        _store = store;
        _clock = clock;
        _options = options.Value;
        ValidateOptions(_options);
        _cursorCodec = new RecommendationCursorCodec(_options);
    }

    public async Task<RecommendationFeedResponse> GetFeedAsync(
        RecommendationFeedRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var anonymousSessionId = request.AnonymousSessionId?.Trim();
        var subjectFingerprint = RecommendationCursorCodec.CreateSubjectFingerprint(
            request.UserId,
            anonymousSessionId);

        Guid requestId;
        int offset;
        DateTimeOffset generatedAt;
        if (string.IsNullOrWhiteSpace(request.Cursor))
        {
            requestId = Guid.NewGuid();
            offset = request.InitialOffset;
            generatedAt = _clock.UtcNow;
        }
        else
        {
            var decoded = _cursorCodec.Decode(
                request.Cursor,
                subjectFingerprint,
                request.CurrentPostId,
                _options.AlgorithmVersion);
            requestId = decoded.RequestId;
            offset = decoded.Offset;
            generatedAt = decoded.GeneratedAt;
        }

        var candidates = await _store.LoadCandidatesAsync(
            request.UserId,
            request.CurrentPostId,
            _options.CandidatePoolSize,
            generatedAt.AddDays(-_options.SeenWindowDays),
            cancellationToken);
        var ranked = Rank(candidates, requestId, generatedAt);
        var diversified = Diversify(ranked);
        var page = SelectPage(diversified, offset, request.Limit);

        var impressions = page.Items.Select((item, position) => RecommendationImpression.Create(
            requestId,
            item.Candidate.PostId,
            request.UserId,
            anonymousSessionId,
            offset + position,
            _options.AlgorithmVersion,
            item.Source,
            item.Score,
            _clock.UtcNow).Value).ToArray();
        if (impressions.Length > 0)
        {
            await _store.SaveImpressionsAsync(impressions, cancellationToken);
        }

        var items = page.Items
            .Select(item => new RecommendationFeedItem(item.Candidate.PostId, ToReason(item.Source)))
            .ToArray();
        var nextCursor = page.NextOffset < diversified.Count
            ? _cursorCodec.Encode(
                requestId,
                page.NextOffset,
                generatedAt,
                subjectFingerprint,
                request.CurrentPostId,
                _options.AlgorithmVersion)
            : null;

        return new RecommendationFeedResponse(
            requestId,
            _options.AlgorithmVersion,
            items,
            nextCursor);
    }

    private IReadOnlyList<RankedRecommendationCandidate> Rank(
        IReadOnlyList<RecommendationCandidateData> candidates,
        Guid requestId,
        DateTimeOffset generatedAt)
    {
        var maxTrending = candidates.Count == 0
            ? 1
            : Math.Max(1, candidates.Max(GetTrendingRaw));

        return candidates
            .Select(candidate => RankCandidate(candidate, requestId, generatedAt, maxTrending))
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Candidate.CreatedAt)
            .ThenBy(x => x.Candidate.PostId)
            .ToArray();
    }

    private RankedRecommendationCandidate RankCandidate(
        RecommendationCandidateData candidate,
        Guid requestId,
        DateTimeOffset generatedAt,
        double maxTrending)
    {
        var category = NormalizeAffinity(candidate.CategoryAffinity);
        var similarity = Math.Clamp(candidate.CurrentPostSimilarity, 0, 1);
        var age = generatedAt - candidate.CreatedAt;
        var trending = GetTrendingRaw(candidate) / maxTrending
            * Math.Pow(0.5, Math.Max(0, age.TotalDays) / 7);
        var subscription = candidate.IsSubscribed ? 1d : NormalizeAffinity(candidate.BlogAffinity);
        var freshness = Math.Pow(0.5, Math.Max(0, age.TotalDays) / 3);
        var exploration = GetDeterministicExploration(requestId, candidate.PostId);

        var contributions = new (CandidateSource Source, double Value)[]
        {
            (CandidateSource.CategoryAffinity, 0.30 * category),
            (CandidateSource.CurrentPostSimilarity, 0.20 * similarity),
            (age.TotalHours <= 24 ? CandidateSource.Trending24Hours : CandidateSource.Trending7Days, 0.18 * trending),
            (candidate.IsSubscribed ? CandidateSource.Subscription : CandidateSource.BlogAffinity, 0.15 * subscription),
            (CandidateSource.Fresh, 0.10 * freshness),
            (CandidateSource.Exploration, 0.07 * exploration)
        };
        var score = contributions.Sum(x => x.Value) - (candidate.WasSeen ? _options.SeenPenalty : 0);
        var source = contributions.MaxBy(x => x.Value).Source;
        return new RankedRecommendationCandidate(candidate, score, source);
    }

    private IReadOnlyList<RankedRecommendationCandidate> Diversify(
        IReadOnlyList<RankedRecommendationCandidate> ranked)
    {
        var result = new List<RankedRecommendationCandidate>(ranked.Count);
        var remaining = ranked.ToList();

        while (remaining.Count > 0)
        {
            var postsPerBlog = new Dictionary<Guid, int>();
            var deferred = new List<RankedRecommendationCandidate>();

            foreach (var candidate in remaining)
            {
                var blogId = candidate.Candidate.BlogId;
                var blogCount = postsPerBlog.GetValueOrDefault(blogId);
                if (blogCount >= _options.MaxPostsPerBlog)
                {
                    deferred.Add(candidate);
                    continue;
                }

                postsPerBlog[blogId] = blogCount + 1;
                result.Add(candidate);
            }

            remaining = deferred;
        }

        return result;
    }

    private static PageSelection SelectPage(
        IReadOnlyList<RankedRecommendationCandidate> ranked,
        int offset,
        int limit)
    {
        var safeOffset = Math.Min(offset, ranked.Count);
        var items = ranked.Skip(safeOffset).Take(limit).ToArray();
        return new PageSelection(items, safeOffset + items.Length);
    }

    private static double GetTrendingRaw(RecommendationCandidateData candidate) =>
        Math.Log(1 + Math.Max(0, candidate.ViewCount + candidate.LikeCount * 2 - candidate.DislikeCount));

    private static double NormalizeAffinity(double value) =>
        value <= 0 ? 0 : 1 - Math.Exp(-value / 10);

    private static double GetDeterministicExploration(Guid requestId, Guid postId)
    {
        Span<byte> input = stackalloc byte[32];
        requestId.TryWriteBytes(input[..16]);
        postId.TryWriteBytes(input[16..]);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(input, hash);
        return BinaryPrimitives.ReadUInt64LittleEndian(hash) / (double)ulong.MaxValue;
    }

    private static string ToReason(CandidateSource source) => source switch
    {
        CandidateSource.Subscription => "subscribed_blog",
        CandidateSource.BlogAffinity => "preferred_blog",
        CandidateSource.CategoryAffinity => "preferred_category",
        CandidateSource.CurrentPostSimilarity => "similar_category",
        CandidateSource.Trending24Hours => "trending_24h",
        CandidateSource.Trending7Days => "trending_7d",
        CandidateSource.Fresh => "fresh",
        CandidateSource.Exploration => "exploration",
        _ => "recommended"
    };

    private void ValidateRequest(RecommendationFeedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hasUser = request.UserId.HasValue && request.UserId.Value != Guid.Empty;
        var hasAnonymous = !string.IsNullOrWhiteSpace(request.AnonymousSessionId);
        if (hasUser == hasAnonymous)
            throw new ArgumentException("Exactly one recommendation subject must be specified.");
        if (request.Limit < 1 || request.Limit > _options.MaxLimit)
            throw new ArgumentOutOfRangeException(nameof(request.Limit), $"Limit must be between 1 and {_options.MaxLimit}.");
        if (request.InitialOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(request.InitialOffset));
        if (!string.IsNullOrWhiteSpace(request.Cursor) && request.InitialOffset != 0)
            throw new ArgumentException("Cursor and initial offset cannot be combined.");
    }

    private static void ValidateOptions(RecommendationFeedOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AlgorithmVersion))
            throw new InvalidOperationException("Recommendation algorithm version is required.");
        if (options.MaxLimit < 1 || options.CandidatePoolSize < options.MaxLimit)
            throw new InvalidOperationException("Recommendation feed limits are invalid.");
        if (options.MaxPostsPerBlog < 1 || options.SeenWindowDays < 1)
            throw new InvalidOperationException("Recommendation diversification options are invalid.");
    }

    private sealed record PageSelection(
        IReadOnlyList<RankedRecommendationCandidate> Items,
        int NextOffset);
}
