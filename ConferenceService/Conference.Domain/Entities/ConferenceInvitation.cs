using Shared.Services;

namespace Conference.Domain.Entities;

public sealed class ConferenceInvitation : IConferenceEntity
{
    private ConferenceInvitation() { }

    public ConferenceInvitation(Guid id, Guid conferenceId, Guid actorUserId, Guid recipientUserId,
        DateTimeOffset expiresAt)
    {
        Id = id;
        ConferenceId = conferenceId;
        ActorUserId = actorUserId;
        RecipientUserId = recipientUserId;
        CreatedAt = DateTimeService.Now().ToUniversalTime();
        ExpiresAt = expiresAt.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid ConferenceId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
}
