using Microsoft.EntityFrameworkCore.Metadata.Internal;
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

        public IReadOnlyList<ConferenceParticipant> Participants { get => _participants; }

        public ConferenceRoom()
        {

        }

        public ConferenceRoom(Guid id, Guid postId, string url, bool isActive, ConferenceParticipant creator)
        {
            Id = id;
            CreatedAt = DateTimeService.Now();
            IsDeleted = false;
            PostId = postId;
            Url = url;
            IsActive = isActive;
            _participants = [creator];
        }

        public void AddParticipant(ConferenceParticipant participant)
        {

            _participants.Add(participant);
        }

        public void RemoveParticipant(ConferenceParticipant participant)
        {

            _participants.Remove(participant);
        }

        public ConferenceRoomKey GetCacheKey() => new(Id);
    }

    public enum Access
    {
        All,
        OnlyAuth
    }

    public readonly struct ConferenceRoomKey
    {
        public const string Key = nameof(ConferenceRoom);

        private readonly Guid _id;

        public ConferenceRoomKey(Guid id)
        {
            _id = id;
        }

        public static implicit operator string(ConferenceRoomKey key) => $"{Key}:{key._id}";
    }
}
