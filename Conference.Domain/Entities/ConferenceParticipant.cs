using System.ComponentModel.DataAnnotations.Schema;

namespace Conference.Domain.Entities
{
    public class ConferenceParticipant : IConferenceEntity
    {
        public Guid? UserId { get; }
        public Guid SessionId { get; }
        public Guid ConferenceRoomId { get; }

        [ForeignKey(nameof(ConferenceRoomId))]
        public ConferenceRoom ConferenceRoom { get; }

        public ConferenceParticipant()
        {
            
        }

        public ConferenceParticipant(Guid? userId, Guid sessionId, Guid conferenceRoomId)
        {
            UserId = userId;
            SessionId = sessionId;
            ConferenceRoomId = conferenceRoomId;
        }
    }
}
