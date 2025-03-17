using Shared;
using Shared.Services;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conference.Domain.Entities
{
    public class ConferenceRoom : BaseEntity<Guid>, IConferenceEntity
    {
        public Guid PostId { get; }
        public string Url { get; set; }
        public bool IsActive { get; set; }
        private List<ConferenceParticipant> _participants { get; set; } = [];

        [NotMapped]
        public IReadOnlyList<ConferenceParticipant> Participants { get => _participants; }

        public ConferenceRoom()
        {
            
        }

        public ConferenceRoom(Guid postId, string url, bool isActive, List<ConferenceParticipant> participants)
        {
            Id = GuidService.GetNewGuid();
            CreatedAt = DateTimeService.Now();
            IsDeleted = false;
            PostId = postId;
            Url = url;
            IsActive = isActive;
            _participants = participants;
        }

        public void AddParticipant(ConferenceParticipant participant)
        {

            _participants.Add(participant);
        }

        public void RemoveParticipant(ConferenceParticipant participant)
        {

            _participants.Remove(participant);
        }
    }
}
