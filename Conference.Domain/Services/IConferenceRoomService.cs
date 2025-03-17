using Conference.Domain.Models;

namespace Conference.Domain.Services
{
    public interface IConferenceRoomService
    {
        Task<CreateConferenceViewModel> CreateConferenceRoom(Guid creatorUserId, Guid postId);
        Task AddParticipantToConference(Guid id, string sessionId);
    }
}
