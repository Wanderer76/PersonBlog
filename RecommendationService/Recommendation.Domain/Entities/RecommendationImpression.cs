using Recommendation.Domain.Enums;
using Shared.Utils;

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

    private RecommendationImpression(
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

    public static Result<RecommendationImpression> Create(
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
        if (requestId == Guid.Empty)
            return new Error(nameof(requestId), "RequestId is required.");
        if (postId == Guid.Empty)
            return new Error(nameof(postId), "PostId is required.");
        if (position < 0)
            return new Error(nameof(position), "Position cannot be negative.");
        if (string.IsNullOrWhiteSpace(algorithmVersion))
            return new Error(nameof(algorithmVersion), "AlgorithmVersion is required.");
        if (!double.IsFinite(score))
            return new Error(nameof(score), "Score must be finite.");

        var hasUser = userId.HasValue && userId.Value != Guid.Empty;
        var hasAnonymousSession = !string.IsNullOrWhiteSpace(anonymousSessionId);
        if (hasUser == hasAnonymousSession)
            return new Error("subject", "Exactly one impression subject must be specified.");

        return new RecommendationImpression(
            requestId,
            postId,
            userId,
            anonymousSessionId,
            position,
            algorithmVersion,
            candidateSource,
            score,
            shownAt);
    }
}
