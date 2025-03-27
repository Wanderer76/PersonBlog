using Conference.Domain.Entities;
using Conference.Domain.Models;
using Conference.Domain.Services;
using Conference.Service.Extensions;
using Conference.Service.Hubs;
using Infrastructure.Extensions;
using Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Conference.Service.Implementation
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
            var conference = await _cacheService.GetConferenceRoomCacheAsync(cacheKey);
            conference ??= await _readWriteRepository.Get<ConferenceRoom>()
                    .Include(x => x.Participants)
                    .FirstAsync(x => x.Id == id);

            if (!conference.Participants.Any(x => x.SessionId == sessionId))
            {
                var session = (await _cacheService.GetUserSessionCachedAsync(sessionId))!;
                conference.AddParticipant(new ConferenceParticipant(session.SessionId, session.UserId, conference.Id));
                await _readWriteRepository.SaveChangesAsync();
                await _cacheService.UpdateConferenceRoomCacheAsync(conference);
            }
        }

        public async Task<ConferenceViewModel> CreateConferenceRoomAsync(Guid sessionId, Guid postId)
        {
            var roomId = GuidService.GetNewGuid();
            var creatorUser = (await _cacheService.GetUserSessionCachedAsync(sessionId))!;
            if (creatorUser.UserId.HasValue)
            {
                var creator = new ConferenceParticipant(sessionId, creatorUser.UserId!.Value, roomId);
                var conference = new ConferenceRoom(roomId, postId, "", true, creator);
                _readWriteRepository.Add(conference);
                await _readWriteRepository.SaveChangesAsync();
                await _cacheService.UpdateConferenceRoomCacheAsync(conference);
                return new ConferenceViewModel(conference.Id, conference.Url, conference.PostId);
            }
            else
            {
                throw new ArgumentException("Пользователь должен быть авторизован");
            }
        }

        public async Task<ConferenceViewModel> GetConferenceRoomByIdAsync(Guid id)
        {
            var conference = await _cacheService.GetConferenceRoomCacheAsync(new ConferenceRoomKey(id));
            if (conference == null)
            {
                conference ??= await _readWriteRepository.Get<ConferenceRoom>()
                        .Include(x => x.Participants)
                        .Where(x => x.IsActive)
                        .FirstAsync(x => x.Id == id);
                await _cacheService.UpdateConferenceRoomCacheAsync(conference);
            }

            if (!conference.IsActive)
                throw new ArgumentException("Conference is close");
            return new ConferenceViewModel(conference.Id, conference.Url, conference.PostId);
        }

        public async ValueTask<bool> IsConferenceActiveAsync(Guid id)
        {
            return await _readWriteRepository.Get<ConferenceRoom>()
                .Where(x => x.Id == id)
                .Select(x => x.IsActive)
                .FirstAsync();
        }

        public async Task RemoveParticipantToConferenceAsync(Guid roomId, Guid sessionId)
        {
            var cacheKey = new ConferenceRoomKey(roomId);
            var conference = await _cacheService.GetConferenceRoomCacheAsync(cacheKey);
            if (conference == null)
            {
                conference ??= await _readWriteRepository.Get<ConferenceRoom>()
                        .Include(x => x.Participants)
                        .FirstAsync(x => x.Id == roomId);
                await _cacheService.UpdateConferenceRoomCacheAsync(conference);
            }

            var userToRemove = conference.Participants.FirstOrDefault(x => x.SessionId == sessionId);
            if (userToRemove != null)
            {
                conference.RemoveParticipant(userToRemove);
                _readWriteRepository.Remove(userToRemove);
                if (conference.Participants.Count == 0)
                {
                    _readWriteRepository.Attach(conference);
                    conference.Close();
                }
                await _readWriteRepository.SaveChangesAsync();
                await _cacheService.UpdateConferenceRoomCacheAsync(conference);
            }
        }
    }
}
