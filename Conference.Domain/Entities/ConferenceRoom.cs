using Shared;
using Shared.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Conference.Domain.Entities
{
    public class ConferenceRoom : BaseEntity, IConferenceEntity
    {
        [Key]
        public Guid Id { get; }
        public Guid PostId { get; }
        public ConferenceState State { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public List<ConferenceParticipant> Participants { get; private set; }

        [NotMapped]
        public bool IsActive { get => State == ConferenceState.Active; }

        public ConferenceRoom()
        {

        }

        [JsonConstructor]
        protected ConferenceRoom(Guid id, Guid postId, ConferenceState state, DateTimeOffset updatedAt, List<ConferenceParticipant> participants)
        {
            Id = id;
            PostId = postId;
            State = state;
            UpdatedAt = updatedAt;
            Participants = participants;
        }

        public ConferenceRoom(Guid id, Guid postId, ConferenceParticipant creator)
        {
            Id = id;
            PostId = postId;
            CreatedAt = DateTimeService.Now();
            UpdatedAt = DateTimeService.Now();
            IsDeleted = false;
            State = ConferenceState.Active;
            Participants = [creator];
        }

        public void AddParticipant(ConferenceParticipant participant)
        {
            Participants.Add(participant);
            UpdatedAt = DateTimeService.Now();
        }

        public void RemoveParticipant(ConferenceParticipant participant)
        {
            Participants.Remove(participant);
            UpdatedAt = DateTimeService.Now();
        }

        public void Close()
        {
            State = ConferenceState.ReadyToRemove;
            IsDeleted = true;
        }

        public ConferenceRoomKey GetCacheKey() => new(Id);
    }

    public enum Access
    {
        All,
        OnlyAuth
    }
    public enum ConferenceState
    {
        Active,
        Unused,
        ReadyToRemove
    }

    public readonly struct ConferenceRoomKey(Guid id) : ICacheKey
    {
        public const string Key = nameof(ConferenceRoom);

        private readonly Guid _id = id;

        public string GetKey() => $"{Key}:{_id}";
    }
}
