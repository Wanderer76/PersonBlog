using Conference.Domain.Models;

namespace Conference.Domain.Services
{
    public interface IConferenceRoomService
    {
        Task<ConferenceViewModel> CreateConferenceRoomAsync(Guid creatorUserId, Guid postId);
        Task<ConferenceViewModel> GetConferenceRoomByIdAsync(Guid id);
        Task AddParticipantToConferenceAsync(Guid id, Guid sessionId);
        Task RemoveParticipantToConferenceAsync(Guid id, Guid sessionId);
    }
}
