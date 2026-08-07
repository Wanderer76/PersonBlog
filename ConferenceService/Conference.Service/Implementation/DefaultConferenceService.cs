using Conference.Domain.Entities;
using Conference.Domain.Models;
using Conference.Domain.Services;
using Conference.Service.Extensions;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Conference.Service.Implementation;

public sealed class DefaultConferenceService(
    IReadWriteRepository<IConferenceEntity> repository,
    ICacheService cacheService,
    ICurrentUserService currentUserService) : IConferenceRoomService
{
    public async Task AddParticipantToConferenceAsync(Guid id, Guid userId, string? userName = null)
    {
        var conference = await repository.Get<ConferenceRoom>()
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (conference == null || !conference.IsActive)
        {
            throw new ArgumentException("Conference does not exist or is closed.", nameof(id));
        }

        if (conference.Participants.Any(x => x.UserId == userId))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            var user = await currentUserService.GetCurrentUserAsync();
            if (user.IsAnonymous || user.UserId != userId)
            {
                throw new UnauthorizedAccessException("The current user session is invalid.");
            }

            userName = user.UserName;
        }

        var participant = new ConferenceParticipant(
            GuidService.GetNewGuid(),
            userId,
            userName,
            conference.Id);
        conference.AddParticipant(participant);
        repository.Add(participant);
        await repository.SaveChangesAsync();
        await cacheService.RemoveConferenceRoomCacheAsync(conference.GetCacheKey());
    }

    public async Task<ConferenceViewModel> CreateConferenceRoomAsync(Guid userId, Guid postId)
    {
        var creatorUser = await currentUserService.GetCurrentUserAsync();
        if (creatorUser.IsAnonymous || creatorUser.UserId != userId)
        {
            throw new UnauthorizedAccessException("An authenticated user is required.");
        }

        var roomId = GuidService.GetNewGuid();
        var creator = new ConferenceParticipant(
            GuidService.GetNewGuid(),
            creatorUser.UserId,
            creatorUser.UserName,
            roomId);
        var conference = new ConferenceRoom(roomId, postId, creator);
        repository.Add(conference);
        await repository.SaveChangesAsync();
        await cacheService.UpdateConferenceRoomCacheAsync(conference);

        return new ConferenceViewModel(conference.Id, conference.PostId);
    }

    public async Task<ConferenceViewModel> GetConferenceRoomByIdAsync(Guid id)
    {
        var cacheKey = new ConferenceRoomKey(id);
        var conference = await cacheService.GetConferenceRoomCacheAsync(cacheKey);
        conference ??= await repository.Get<ConferenceRoom>()
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == id && x.State == ConferenceState.Active);

        if (conference == null)
        {
            throw new ArgumentException("Conference does not exist or is closed.", nameof(id));
        }

        if ((DateTimeService.Now() - conference.UpdatedAt).TotalMinutes > 10 && conference.Participants.Count == 0)
        {
            var persistedConference = await repository.Get<ConferenceRoom>()
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (persistedConference != null && persistedConference.IsActive && persistedConference.Participants.Count == 0)
            {
                persistedConference.Close();
                await repository.SaveChangesAsync();
            }

            await cacheService.RemoveConferenceRoomCacheAsync(cacheKey);
            throw new InvalidOperationException("Conference is closed.");
        }

        if (!conference.IsActive)
        {
            throw new InvalidOperationException("Conference is closed.");
        }

        await cacheService.UpdateConferenceRoomCacheAsync(conference);
        return new ConferenceViewModel(conference.Id, conference.PostId);
    }

    public async ValueTask<bool> IsConferenceActiveAsync(Guid id)
    {
        return await repository.Get<ConferenceRoom>()
            .AnyAsync(x => x.Id == id && x.State == ConferenceState.Active);
    }

    public async Task RemoveParticipantToConferenceAsync(Guid roomId, Guid userId)
    {
        var conference = await repository.Get<ConferenceRoom>()
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == roomId);
        if (conference == null)
        {
            return;
        }

        var participant = conference.Participants.FirstOrDefault(x => x.UserId == userId);
        if (participant == null)
        {
            return;
        }

        conference.RemoveParticipant(participant);
        repository.Remove(participant);
        await repository.SaveChangesAsync();
        await cacheService.RemoveConferenceRoomCacheAsync(conference.GetCacheKey());
    }
}
