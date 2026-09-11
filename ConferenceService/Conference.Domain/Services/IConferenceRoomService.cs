using Conference.Domain.Models;

using Shared.Models;

namespace Conference.Domain.Services
{
    public interface IConferenceRoomService
    {
        Task<ConferenceViewModel> CreateConferenceRoomAsync(Guid creatorUserId, Guid postId);
        Task<Result<ConferenceInvitationViewModel>> CreateInvitationAsync(Guid conferenceId,
            CreateConferenceInvitationRequest request, CancellationToken cancellationToken = default);
        Task<ConferenceViewModel> GetConferenceRoomByIdAsync(Guid id);
        Task AddParticipantToConferenceAsync(Guid id, Guid userId, string? userName = null);
        Task RemoveParticipantToConferenceAsync(Guid id, Guid userId);
        ValueTask<bool> IsConferenceActiveAsync(Guid id);
    }
}
