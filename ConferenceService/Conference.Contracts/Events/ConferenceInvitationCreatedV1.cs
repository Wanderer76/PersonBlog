namespace Conference.Contracts.Events;

public static class ConferenceIntegrationEvents
{
    public const string Exchange = "conference.events";
    public const string ConferenceInvitationCreatedV1RoutingKey = "conference.invitation-created.v1";
}

public sealed record ConferenceInvitationCreatedV1
{
    public const int CurrentSchemaVersion = 1;
    public const string ProducerName = "Conference";

    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string Producer { get; init; } = ProducerName;
    public required Guid InvitationId { get; init; }
    public required Guid ConferenceId { get; init; }
    public required Guid ActorUserId { get; init; }
    public required Guid RecipientUserId { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
