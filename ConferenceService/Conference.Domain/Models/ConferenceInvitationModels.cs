namespace Conference.Domain.Models;

public sealed record CreateConferenceInvitationRequest(Guid RecipientUserId, DateTimeOffset? ExpiresAt = null);
public sealed record ConferenceInvitationViewModel(Guid Id, Guid ConferenceId, Guid RecipientUserId,
    DateTimeOffset ExpiresAt);
