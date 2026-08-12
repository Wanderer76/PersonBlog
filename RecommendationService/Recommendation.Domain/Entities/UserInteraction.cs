using Recommendation.Domain.Enums;

namespace Recommendation.Domain.Entities;

public sealed class UserInteraction : IRecommendationEntity
{
    public Guid EventId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? AnonymousSessionId { get; private set; }
    public Guid PostId { get; private set; }
    public InteractionType InteractionType { get; private set; }
    public double? WatchedSeconds { get; private set; }
    public double? WatchRatio { get; private set; }
    public bool? Reaction { get; private set; }
    public bool? PreviousReaction { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private UserInteraction()
    {
    }

    public UserInteraction(
        Guid eventId,
        Guid? userId,
        string? anonymousSessionId,
        Guid postId,
        InteractionType interactionType,
        DateTimeOffset occurredAt,
        double? watchedSeconds = null,
        double? watchRatio = null,
        bool? reaction = null,
        bool? previousReaction = null)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("EventId is required.", nameof(eventId));
        if (postId == Guid.Empty) throw new ArgumentException("PostId is required.", nameof(postId));
        ValidateSubject(userId, anonymousSessionId);
        if (watchedSeconds is < 0) throw new ArgumentOutOfRangeException(nameof(watchedSeconds));
        if (watchRatio is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(watchRatio));
        ValidateReaction(interactionType, reaction);

        EventId = eventId;
        UserId = userId;
        AnonymousSessionId = anonymousSessionId?.Trim();
        PostId = postId;
        InteractionType = interactionType;
        OccurredAt = occurredAt;
        WatchedSeconds = watchedSeconds;
        WatchRatio = watchRatio;
        Reaction = reaction;
        PreviousReaction = previousReaction;
    }

    private static void ValidateSubject(Guid? userId, string? anonymousSessionId)
    {
        var hasUser = userId.HasValue && userId.Value != Guid.Empty;
        var hasAnonymousSession = !string.IsNullOrWhiteSpace(anonymousSessionId);
        if (hasUser == hasAnonymousSession)
            throw new ArgumentException("Exactly one interaction subject must be specified.");
    }

    private static void ValidateReaction(InteractionType interactionType, bool? reaction)
    {
        if (interactionType == InteractionType.Like && reaction != true)
            throw new ArgumentException("Like interaction must have a positive reaction.", nameof(reaction));
        if (interactionType == InteractionType.Dislike && reaction != false)
            throw new ArgumentException("Dislike interaction must have a negative reaction.", nameof(reaction));
        if (interactionType == InteractionType.ReactionRemoved && reaction is not null)
            throw new ArgumentException("Removed reaction must be null.", nameof(reaction));
    }
}
