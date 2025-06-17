using Conference.Domain.Models;

namespace Conference.Domain.Services
{
    public interface IConferenceRoomService
    {
        Task<ConferenceViewModel> CreateConferenceRoomAsync(Guid creatorUserId, Guid postId);
        Task<ConferenceViewModel> GetConferenceRoomByIdAsync(Guid id);
        Task AddParticipantToConferenceAsync(Guid id, Guid userId);
        Task RemoveParticipantToConferenceAsync(Guid id, Guid userId);
        ValueTask<bool> IsConferenceActiveAsync(Guid id);
    }
}
