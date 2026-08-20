using MessageBus.EventHandler;
using Recommendation.Contracts.Events;
using Recommendation.Domain.Entities;
using Recommendation.Domain.Models;
using Recommendation.Services.Abstractions;
using DomainPostProcessState = Recommendation.Domain.Enums.PostProcessState;
using DomainPostType = Recommendation.Domain.Enums.PostType;
using DomainPostVisibility = Recommendation.Domain.Enums.PostVisibility;

namespace Recommendation.Services.EventHandlers;

public sealed class PostCatalogChangedV2Handler(
    IRecommendationEventStore store,
    IClock clock) : IEventHandler<PostCatalogChangedV2>
{
    public Task Handle(IMessageContext<PostCatalogChangedV2> @event) => HandleAsync(@event.Message);

    public Task HandleAsync(PostCatalogChangedV2 message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var inbox = InboxMessage.Create(message.EventId, nameof(PostCatalogChangedV2), clock.UtcNow).Value;

        return store.ExecuteOnceAsync(inbox, async (session, token) =>
        {
            var data = Map(message);
            var snapshot = await session.GetPostSnapshotAsync(message.PostId, token);
            if (snapshot is null)
            {
                session.AddPostSnapshot(PostSnapshot.Create(data));
                return;
            }

            snapshot.TryApply(data);
        }, cancellationToken);
    }

    private static PostSnapshotData Map(PostCatalogChangedV2 message) => new()
    {
        PostId = message.PostId,
        SourceVersion = message.AggregateVersion,
        BlogId = message.BlogId,
        PostType = message.PostType switch
        {
            RecommendationPostType.Text => DomainPostType.Text,
            RecommendationPostType.Video => DomainPostType.Video,
            _ => throw new ArgumentOutOfRangeException(nameof(message.PostType))
        },
        Title = message.Title,
        Description = message.Description,
        CategoryIds = message.CategoryIds,
        Visibility = message.Visibility switch
        {
            RecommendationPostVisibility.Public => DomainPostVisibility.Public,
            RecommendationPostVisibility.ByUrl => DomainPostVisibility.ByUrl,
            RecommendationPostVisibility.Private => DomainPostVisibility.Private,
            _ => throw new ArgumentOutOfRangeException(nameof(message.Visibility))
        },
        ProcessState = message.ProcessState switch
        {
            RecommendationProcessState.Draft => DomainPostProcessState.Draft,
            RecommendationProcessState.Complete => DomainPostProcessState.Complete,
            RecommendationProcessState.Load => DomainPostProcessState.Load,
            RecommendationProcessState.Error => DomainPostProcessState.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(message.ProcessState))
        },
        IsDeleted = message.IsDeleted,
        IsBanned = message.IsBanned,
        PaymentSubscriptionId = message.PaymentSubscriptionId,
        PreviewObjectName = message.PreviewObjectName,
        DurationSeconds = message.DurationSeconds,
        CreatedAt = message.CreatedAt,
        ViewCount = message.ViewCount,
        LikeCount = message.LikeCount,
        DislikeCount = message.DislikeCount,
        UpdatedAt = message.OccurredAt
    };
}
