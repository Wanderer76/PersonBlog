﻿using Conference.Domain.Entities;
using Conference.Domain.Models;
using Conference.Domain.Services;
using Conference.Service.Extensions;
using Infrastructure.Extensions;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Conference.Service.Implementation
{
    internal class DefaultConferenceChatService : IConferenceChatService
    {
        private readonly IReadWriteRepository<IConferenceEntity> _readWriteRepository;
        private readonly ICacheService _cacheService;

        public DefaultConferenceChatService(IReadWriteRepository<IConferenceEntity> readWriteRepository, ICacheService cacheService)
        {
            _readWriteRepository = readWriteRepository;
            _cacheService = cacheService;
        }

        public async Task<MessageModel> CreateMessageAsync(Guid sessionId, CreateMessageForm messageForm)
        {
            var conference = await _cacheService.GetConferenceRoomCacheAsync(new ConferenceRoomKey(messageForm.ConferenceId));
            if (conference == null)
            {
                conference = await _readWriteRepository.Get<ConferenceRoom>()
                    .Include(x => x.Participants)
                    .FirstOrDefaultAsync(x => x.Id == messageForm.ConferenceId);
                await _cacheService.UpdateConferenceRoomCacheAsync(conference);
            }
            if (conference == null)
                throw new ArgumentException("no such conference");

            var user = await _cacheService.GetUserSessionCachedAsync(sessionId);

            user.AssertFound();

            var message = new Message(GuidService.GetNewGuid(), conference.Id, user.UserId!.Value, messageForm.Message);
            _readWriteRepository.Add(message);
            await _readWriteRepository.SaveChangesAsync();

            var result = new MessageModel(user.UserName, message.MessageText, message.CreatedAt.DateTime, null);

            var key = new MessageModelCacheKey(conference.Id);

            var allData = (await _cacheService.GetCachedDataAsync<List<MessageModel>>(key)) ?? [];
            allData.Add(result);

            await _cacheService.SetCachedDataAsync(key, allData, TimeSpan.FromHours(1));
            return result;
        }

        public async Task<IReadOnlyList<MessageModel>> GetLastMessagesAsync(Guid conferenceId, int offset, int limit)
        {
            var conference = await _cacheService.GetConferenceRoomCacheAsync(new ConferenceRoomKey(conferenceId));
            conference ??= await _readWriteRepository.Get<ConferenceRoom>()
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.Id == conferenceId);

            if (conference == null)
                throw new ArgumentException("no such conference");

            var participantsUserNames = (await _cacheService.GetCachedDataAsync<UserModel>(conference.Participants.Select(x => new SessionKey(x.SessionId).GetKey())))
                .Where(x => x.UserId.HasValue)
                .ToDictionary(x => x.UserId!.Value, x => x.UserName);


            var total = offset * limit;
            var key = new MessageModelCacheKey(conferenceId);
            var messages = await _cacheService.GetCachedDataAsync<List<MessageModel>>(key);
            if (messages == null)
            {
                var allMessages = await _readWriteRepository.Get<Message>().Where(x => x.ConferenceId == conferenceId).CountAsync();
                var allPages = allMessages / limit;
                if (allPages < offset)
                    return [];

                messages = await _readWriteRepository.Get<Message>()
                    .Where(x => x.ConferenceId == conferenceId)
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip(offset)
                    .Take(limit)
                    .AsAsyncEnumerable()
                    .Select(x =>
                    {
                        var userName = participantsUserNames.TryGetValue(x.CreatorId, out var name) ? name ?? "unknown" : "unknown";
                        return new MessageModel(userName, x.MessageText, x.CreatedAt.DateTime, null);
                    })
                    .ToListAsync();
                await _cacheService.SetCachedDataAsync(key, messages, TimeSpan.FromHours(1));
                return messages;
            }
            else
            {
                var allPages = Math.Max(messages.Count / limit, 1);
                if (allPages < offset)
                    return [];
                return messages.OrderByDescending(x => x.CreatedAt).Skip(offset).Take(limit).OrderBy(x => x.CreatedAt).ToList();
            }

        }
    }

    readonly struct MessageModelCacheKey(Guid conferenceId) : ICacheKey
    {
        private const string Key = nameof(MessageModel);
        private readonly Guid _conferenceId = conferenceId;

        public string GetKey() => $"{Key}:{_conferenceId}";
    }
}
