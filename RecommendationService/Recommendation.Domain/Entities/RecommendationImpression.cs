using Recommendation.Domain.Enums;

namespace Recommendation.Domain.Entities;

public sealed class RecommendationImpression : IRecommendationEntity
{
    public Guid RequestId { get; private set; }
    public Guid PostId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? AnonymousSessionId { get; private set; }
    public int Position { get; private set; }
    public string AlgorithmVersion { get; private set; } = null!;
    public CandidateSource CandidateSource { get; private set; }
    public double Score { get; private set; }
    public DateTimeOffset ShownAt { get; private set; }

    private RecommendationImpression()
    {
    }

    public RecommendationImpression(
        Guid requestId,
        Guid postId,
        Guid? userId,
        string? anonymousSessionId,
        int position,
        string algorithmVersion,
        CandidateSource candidateSource,
        double score,
        DateTimeOffset shownAt)
    {
        if (requestId == Guid.Empty) throw new ArgumentException("RequestId is required.", nameof(requestId));
        if (postId == Guid.Empty) throw new ArgumentException("PostId is required.", nameof(postId));
        if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
        if (string.IsNullOrWhiteSpace(algorithmVersion)) throw new ArgumentException("AlgorithmVersion is required.", nameof(algorithmVersion));
        if (!double.IsFinite(score)) throw new ArgumentOutOfRangeException(nameof(score));

        var hasUser = userId.HasValue && userId.Value != Guid.Empty;
        var hasAnonymousSession = !string.IsNullOrWhiteSpace(anonymousSessionId);
        if (hasUser == hasAnonymousSession)
            throw new ArgumentException("Exactly one impression subject must be specified.");

        RequestId = requestId;
        PostId = postId;
        UserId = userId;
        AnonymousSessionId = anonymousSessionId?.Trim();
        Position = position;
        AlgorithmVersion = algorithmVersion.Trim();
        CandidateSource = candidateSource;
        Score = score;
        ShownAt = shownAt;
    }
}
