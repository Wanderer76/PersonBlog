using System.ComponentModel.DataAnnotations.Schema;

namespace Conference.Domain.Entities
{
    public class ConferenceParticipant : IConferenceEntity
    {
        public Guid? UserId { get; }
        public string SessionId { get; }
        public Guid ConferenceRoomId { get; }

        [ForeignKey(nameof(ConferenceRoomId))]
        public ConferenceRoom ConferenceRoom { get; }

        public ConferenceParticipant(Guid? userId, string sessionId, Guid conferenceRoomId)
        {
            UserId = userId;
            SessionId = sessionId;
            ConferenceRoomId = conferenceRoomId;
        }
    }
}
