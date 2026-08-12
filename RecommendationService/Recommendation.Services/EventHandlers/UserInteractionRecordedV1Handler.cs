using MessageBus.EventHandler;
using Recommendation.Contracts.Events;
using Recommendation.Domain.Entities;
using Recommendation.Domain.Enums;
using Recommendation.Services.Abstractions;
using Recommendation.Services.Services;

namespace Recommendation.Services.EventHandlers;

public sealed class UserInteractionRecordedV1Handler(
    IRecommendationEventStore store,
    IClock clock,
    AffinityCalculator affinityCalculator) : IEventHandler<UserInteractionRecordedV1>
{
    public Task Handle(IMessageContext<UserInteractionRecordedV1> @event) => HandleAsync(@event.Message);

    public Task HandleAsync(UserInteractionRecordedV1 message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var interaction = Map(message);
        var inbox = new InboxMessage(message.EventId, nameof(UserInteractionRecordedV1), clock.UtcNow);

        return store.ExecuteOnceAsync(inbox, async (session, token) =>
        {
            session.AddInteraction(interaction);
            if (!message.UserId.HasValue)
            {
                return;
            }

            var post = await session.GetPostSnapshotAsync(message.PostId, token);
            if (post is null)
            {
                return;
            }

            var delta = affinityCalculator.GetDelta(message);
            if (delta == 0)
            {
                return;
            }

            await UpdateBlogAffinityAsync(session, message.UserId.Value, post.BlogId, delta, message.OccurredAt, token);
            foreach (var categoryId in post.Categories.Select(x => x.CategoryId))
            {
                await UpdateCategoryAffinityAsync(session, message.UserId.Value, categoryId, delta, message.OccurredAt, token);
            }
        }, cancellationToken);
    }

    private async Task UpdateBlogAffinityAsync(
        IRecommendationEventSession session,
        Guid userId,
        Guid blogId,
        double delta,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var affinity = await session.GetBlogAffinityAsync(userId, blogId, cancellationToken);
        if (affinity is null)
        {
            session.AddBlogAffinity(new UserBlogAffinity(userId, blogId, delta, occurredAt));
            return;
        }

        var updated = affinityCalculator.Apply(affinity.Score, affinity.UpdatedAt, delta, occurredAt);
        affinity.Update(updated.Score, updated.UpdatedAt);
    }

    private async Task UpdateCategoryAffinityAsync(
        IRecommendationEventSession session,
        Guid userId,
        int categoryId,
        double delta,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var affinity = await session.GetCategoryAffinityAsync(userId, categoryId, cancellationToken);
        if (affinity is null)
        {
            session.AddCategoryAffinity(new UserCategoryAffinity(userId, categoryId, delta, occurredAt));
            return;
        }

        var updated = affinityCalculator.Apply(affinity.Score, affinity.UpdatedAt, delta, occurredAt);
        affinity.Update(updated.Score, updated.UpdatedAt);
    }

    private static UserInteraction Map(UserInteractionRecordedV1 message) => new(
        message.EventId,
        message.UserId,
        message.AnonymousSessionId,
        message.PostId,
        message.Type switch
        {
            UserInteractionType.Impression => InteractionType.Impression,
            UserInteractionType.Open => InteractionType.Open,
            UserInteractionType.ViewProgress => InteractionType.ViewProgress,
            UserInteractionType.ViewCompleted => InteractionType.ViewCompleted,
            UserInteractionType.Like => InteractionType.Like,
            UserInteractionType.Dislike => InteractionType.Dislike,
            UserInteractionType.ReactionRemoved => InteractionType.ReactionRemoved,
            _ => throw new ArgumentOutOfRangeException(nameof(message.Type))
        },
        message.OccurredAt,
        message.WatchedSeconds,
        message.WatchRatio,
        message.Reaction,
        message.PreviousReaction);
}
