using Conference.Contracts.Events;
using MessageBus.EventHandler;
using Notification.Application.Abstractions;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Messaging;

public sealed class ConferenceInvitationCreatedV1Handler(INotificationWorkStore workStore)
    : IEventHandler<ConferenceInvitationCreatedV1>
{
    public const string ConsumerName = "notification.conference-invitation-created.v1";

    public async Task Handle(IMessageContext<ConferenceInvitationCreatedV1> context)
    {
        var result = await HandleAsync(context.Message);
        if (result.IsFailure) throw new InvalidOperationException($"Conference invitation intake failed: {result.Errors[0].Key}");
    }

    public Task<Result> HandleAsync(ConferenceInvitationCreatedV1 message, CancellationToken cancellationToken = default)
    {
        if (message.SchemaVersion != ConferenceInvitationCreatedV1.CurrentSchemaVersion ||
            message.Producer != ConferenceInvitationCreatedV1.ProducerName || message.EventId == Guid.Empty ||
            message.InvitationId == Guid.Empty || message.ConferenceId == Guid.Empty ||
            message.ActorUserId == Guid.Empty || message.RecipientUserId == Guid.Empty ||
            message.ExpiresAt <= message.OccurredAt)
            return Task.FromResult(Result.Failure("ConferenceInvitation.InvalidContract", "The invitation event is invalid."));

        var content = new NotificationContent(NotificationKind.ConferenceInvitation, message.InvitationId,
            message.ActorUserId,
            new NotificationTarget(NotificationTargetType.ConferenceInvitation, message.InvitationId),
            "conference.invitation", 1,
            new Dictionary<string, string> { ["conferenceId"] = message.ConferenceId.ToString() },
            message.OccurredAt.ToUniversalTime(), message.ExpiresAt.ToUniversalTime());
        return workStore.EnqueueAsync(new NotificationIngress(
            new NotificationSource(message.Producer, message.EventId), ConsumerName, content,
            RecipientUserId: message.RecipientUserId), cancellationToken);
    }
}
