using Recommendation.Domain.Enums;
using Shared.Utils;

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

    private UserInteraction(
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

    public static Result<UserInteraction> Create(
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
        if (eventId == Guid.Empty)
            return new Error(nameof(eventId), "EventId is required.");
        if (postId == Guid.Empty)
            return new Error(nameof(postId), "PostId is required.");

        var hasUser = userId.HasValue && userId.Value != Guid.Empty;
        var hasAnonymousSession = !string.IsNullOrWhiteSpace(anonymousSessionId);
        if (hasUser == hasAnonymousSession)
            return new Error("subject", "Exactly one interaction subject must be specified.");
        if (watchedSeconds is < 0)
            return new Error(nameof(watchedSeconds), "WatchedSeconds cannot be negative.");
        if (watchRatio is < 0 or > 1)
            return new Error(nameof(watchRatio), "WatchRatio must be between 0 and 1.");
        if (interactionType == InteractionType.Like && reaction != true)
            return new Error(nameof(reaction), "Like interaction must have a positive reaction.");
        if (interactionType == InteractionType.Dislike && reaction != false)
            return new Error(nameof(reaction), "Dislike interaction must have a negative reaction.");
        if (interactionType == InteractionType.ReactionRemoved && reaction is not null)
            return new Error(nameof(reaction), "Removed reaction must be null.");

        return new UserInteraction(
            eventId,
            userId,
            anonymousSessionId,
            postId,
            interactionType,
            occurredAt,
            watchedSeconds,
            watchRatio,
            reaction,
            previousReaction);
    }
}
