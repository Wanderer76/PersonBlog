using Shared.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conference.Domain.Entities
{
    public class Message : IConferenceEntity
    {
        [Key]
        public Guid Id { get; set; }
        public Guid ConferenceId { get; set; }

        public Guid CreatorId { get; set; }

        public string MessageText { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public Message()
        {

        }

        public Message(Guid id, Guid conferenceId, Guid creatorId, string messageText)
        {
            Id = id;
            ConferenceId = conferenceId;
            CreatorId = creatorId;
            MessageText = messageText;
            CreatedAt = DateTimeService.Now();
        }

        [ForeignKey(nameof(ConferenceId))]
        public ConferenceRoom Room { get; set; }
    }
}
