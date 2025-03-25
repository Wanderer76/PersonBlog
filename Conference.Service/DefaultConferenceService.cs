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

namespace Conference.Service
{
    public class DefaultConferenceService : IConferenceRoomService
    {
        private readonly IReadWriteRepository<IConferenceEntity> _readWriteRepository;
        private readonly ICacheService _cacheService;
        private readonly IHubContext<ConferenceHub, IConferenceHub> hubContext;

        public DefaultConferenceService(IReadWriteRepository<IConferenceEntity> readWriteRepository, ICacheService cacheService, IHubContext<ConferenceHub, IConferenceHub> hubContext)
        {
            _readWriteRepository = readWriteRepository;
            _cacheService = cacheService;
            this.hubContext = hubContext;
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
            await hubContext.Clients.All.OnConferenceConnect($"Присоединилось пользователей: {conference.Participants.Count}");
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
                        .FirstAsync(x => x.Id == id);
                await _cacheService.UpdateConferenceRoomCacheAsync(conference);
            }
            return new ConferenceViewModel(conference.Id, conference.Url, conference.PostId);
        }

        public async Task RemoveParticipantToConferenceAsync(Guid roomId, Guid sessionId)
        {
            var conference = await _cacheService.GetConferenceRoomCacheAsync(new ConferenceRoomKey(roomId));
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
                await _readWriteRepository.SaveChangesAsync();
                await _cacheService.UpdateConferenceRoomCacheAsync(conference);
            }
        }
    }
}
