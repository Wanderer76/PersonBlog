using Shared;
using Shared.Services;

namespace Conference.Domain.Entities
{
    public class ConferenceRoom : BaseEntity, IConferenceEntity
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public string Url { get; set; }
        public bool IsActive { get; set; }
        public List<ConferenceParticipant> Participants { get; set; }

        public ConferenceRoom()
        {

        }

        public ConferenceRoom(Guid id, Guid postId, string url, bool isActive, ConferenceParticipant creator)
        {
            Id = id;
            PostId = postId;
            CreatedAt = DateTimeService.Now();
            IsDeleted = false;
            Url = url;
            IsActive = isActive;
            Participants = [creator];
        }

        public void AddParticipant(ConferenceParticipant participant)
        {

            Participants.Add(participant);
        }

        public void RemoveParticipant(ConferenceParticipant participant)
        {

            Participants.Remove(participant);
        }

        public void Close()
        {
            IsActive = false;
            IsDeleted = true;
        }

        public ConferenceRoomKey GetCacheKey() => new(Id);
    }

    public enum Access
    {
        All,
        OnlyAuth
    }

    public readonly struct ConferenceRoomKey(Guid id) : ICacheKey
    {
        public const string Key = nameof(ConferenceRoom);

        private readonly Guid _id = id;

        public string GetKey() => $"{Key}:{_id}";

        //public static implicit operator string(ConferenceRoomKey key) => key.GetKey();
    }
}
