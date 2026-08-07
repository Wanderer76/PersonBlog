using Conference.Domain.Entities;
using Conference.Domain.Models;
using Conference.Domain.Services;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;

namespace Conference.Service.Implementation;

internal sealed class DefaultConferenceChatService(
    IReadWriteRepository<IConferenceEntity> repository,
    ICacheService cacheService,
    ICurrentUserService currentUserService) : IConferenceChatService
{
    private const int MaxPageSize = 100;

    public async Task<MessageModel> CreateMessageAsync(Guid userId, CreateMessageForm messageForm)
    {
        var messageText = messageForm.Message?.Trim();
        if (string.IsNullOrEmpty(messageText) || messageText.Length > 4000)
        {
            throw new ArgumentException("Message must contain between 1 and 4000 characters.", nameof(messageForm));
        }

        var conference = await repository.Get<ConferenceRoom>()
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == messageForm.ConferenceId);

        if (conference == null)
        {
            throw new ArgumentException("No such conference.", nameof(messageForm));
        }

        if (!conference.IsActive)
        {
            throw new InvalidOperationException("Conference is closed.");
        }

        if (!conference.Participants.Any(x => x.UserId == userId))
        {
            throw new UnauthorizedAccessException("Join the conference before sending messages.");
        }

        var user = await currentUserService.GetCurrentUserAsync();
        if (user.IsAnonymous || user.UserId != userId)
        {
            throw new UnauthorizedAccessException("The current user session is invalid.");
        }

        var message = new Message(
            GuidService.GetNewGuid(),
            conference.Id,
            user.UserId,
            messageText);
        repository.Add(message);
        await repository.SaveChangesAsync();

        return new MessageModel(user.UserName, message.MessageText, message.CreatedAt.DateTime, null);
    }

    public async Task<IReadOnlyList<MessageModel>> GetLastMessagesAsync(Guid conferenceId, int offset, int limit)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (limit is <= 0 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), $"Limit must be between 1 and {MaxPageSize}.");
        }

        var conference = await repository.Get<ConferenceRoom>()
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == conferenceId);
        if (conference == null)
        {
            throw new ArgumentException("No such conference.", nameof(conferenceId));
        }

        var messages = await repository.Get<Message>()
            .Where(x => x.ConferenceId == conferenceId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        var userKeys = messages
            .Select(x => x.CreatorId)
            .Distinct()
            .Select(x => new SessionKey(x));
        var creators = await cacheService.GetCachedDataAsync<UserModel>(userKeys);
        var creatorNames = creators
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => x.First().UserName);

        return messages
            .Select(x => new MessageModel(
                creatorNames.TryGetValue(x.CreatorId, out var name) ? name ?? "unknown" : "unknown",
                x.MessageText,
                x.CreatedAt.DateTime,
                null))
            .Reverse()
            .ToList();
    }
}
