using Conference.Domain.Entities;
using Conference.Domain.Models;
using Conference.Domain.Services;
using Infrastructure.Interface;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;

namespace Conference.Service
{
    public class DefaultConferenceService : IConferenceRoomService
    {
        private readonly IReadWriteRepository<IConferenceEntity> _readWriteRepository;
        private readonly ICacheService _cacheService;

        public DefaultConferenceService(IReadWriteRepository<IConferenceEntity> readWriteRepository, ICacheService cacheService)
        {
            _readWriteRepository = readWriteRepository;
            _cacheService = cacheService;
        }

        public async Task AddParticipantToConferenceAsync(Guid id, Guid sessionId)
        {
            var cacheKey = new ConferenceRoomKey(id);
            var conference = await _cacheService.GetCachedDataAsync<ConferenceRoom>(cacheKey);
            conference ??= await _readWriteRepository.Get<ConferenceRoom>()
                    .Include(x => x.Participants)
                    .FirstAsync(x => x.Id == id);

            if (!conference.Participants.Any(x => x.SessionId == sessionId))
            {
                var session = await _cacheService.GetCachedDataAsync<UserSession>(new SessionKey(sessionId));
                conference.AddParticipant(new ConferenceParticipant(session.UserId, session.SessionId, conference.Id));
                await _readWriteRepository.SaveChangesAsync();
                await _cacheService.SetCachedDataAsync(cacheKey, conference, TimeSpan.FromMinutes(10));
            }
        }

        public Task<ConferenceViewModel> CreateConferenceRoomAsync(Guid creatorUserId, Guid postId)
        {
            throw new NotImplementedException();
        }

        public Task<ConferenceViewModel> GetConferenceRoomByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task RemoveParticipantToConferenceAsync(Guid id, Guid sessionId)
        {
            throw new NotImplementedException();
        }
    }
}
