using Conference.Domain.Entities;
using Conference.Domain.Models;
using Conference.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Conference.Service
{
    public class DefaultConferenceService : IConferenceRoomService
    {
        private readonly IReadWriteRepository<IConferenceEntity> _readWriteRepository;

        public DefaultConferenceService(IReadWriteRepository<IConferenceEntity> readWriteRepository)
        {
            _readWriteRepository = readWriteRepository;
        }

        public async Task AddParticipantToConferenceAsync(Guid id, Guid sessionId)
        {
            var a = await _readWriteRepository.Get<ConferenceRoom>()
                .ToListAsync();
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
