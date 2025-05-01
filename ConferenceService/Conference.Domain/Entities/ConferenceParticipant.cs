using System.ComponentModel.DataAnnotations.Schema;

namespace Conference.Domain.Entities
{
    public class ConferenceParticipant : IConferenceEntity
    {
        public Guid SessionId { get; set; }

        public Guid? UserId { get; set; }
        public Guid ConferenceRoomId { get; set; }

        [ForeignKey(nameof(ConferenceRoomId))]
        public ConferenceRoom ConferenceRoom { get; set; }

        public ConferenceParticipant()
        {
            
        }

        public ConferenceParticipant(Guid sessionId, Guid? userId, Guid conferenceRoomId)
        {
            UserId = userId;
            SessionId = sessionId;
            ConferenceRoomId = conferenceRoomId;
        }
    }
}
