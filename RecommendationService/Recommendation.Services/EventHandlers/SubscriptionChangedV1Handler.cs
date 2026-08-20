using MessageBus.EventHandler;
using Recommendation.Contracts.Events;
using Recommendation.Domain.Entities;
using Recommendation.Services.Abstractions;

namespace Recommendation.Services.EventHandlers;

public sealed class SubscriptionChangedV1Handler(
    IRecommendationEventStore store,
    IClock clock) : IEventHandler<SubscriptionChangedV1>
{
    public Task Handle(IMessageContext<SubscriptionChangedV1> @event) => HandleAsync(@event.Message);

    public Task HandleAsync(SubscriptionChangedV1 message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var inbox = InboxMessage.Create(message.EventId, nameof(SubscriptionChangedV1), clock.UtcNow).Value;

        return store.ExecuteOnceAsync(inbox, async (session, token) =>
        {
            var current = await session.GetSubscriptionAsync(message.UserId, message.BlogId, token);
            if (message.IsSubscribed)
            {
                if (current is null)
                {
                    session.AddSubscription(UserSubscription.Create(
                        message.UserId,
                        message.BlogId,
                        message.OccurredAt).Value);
                }
                return;
            }

            if (current is not null)
            {
                session.RemoveSubscription(current);
            }
        }, cancellationToken);
    }
}
