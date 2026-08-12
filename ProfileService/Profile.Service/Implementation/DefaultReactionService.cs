using Blog.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Domain.Events;
using Recommendation.Contracts.Events;
using Shared.Persistence;
using Shared.Services;

namespace Profile.Service.Implementation
{
    internal class DefaultReactionService : IReactionService
    {
        private readonly IReadWriteRepository<IUserEntity> _context;

        public DefaultReactionService(IReadWriteRepository<IUserEntity> context)
        {
            _context = context;
        }

        public Task RemoveReactionToPost(Guid postId)
        {
            return Task.CompletedTask;
        }

        public async Task SetReactionToPost(ReactionCreateModel reaction)
        {
            var hasView = await _context.Get<PostReaction>()
              .Where(x => x.PostId == reaction.PostId)
              .Where(x => reaction.UserId.HasValue
                  ? x.UserId == reaction.UserId
                  : x.UserId == null && x.IpAddress == reaction.RemoteIp)
              .FirstOrDefaultAsync();

            var resultingReaction = hasView?.IsLike == reaction.IsLike
                ? null
                : reaction.IsLike;
            var previousReaction = hasView?.IsLike;

            if (hasView == null)
            {
                hasView = new PostReaction(reaction.UserId, reaction.RemoteIp!, reaction.PostId, reaction.Time, resultingReaction);
                _context.Add(hasView);
            }
            else
            {
                _context.Attach(hasView);
                hasView.IsLike = resultingReaction;
            }

            var eventData = new UserReactionSyncEvent
            {
                EventId = GuidService.GetNewGuid(),
                PostId = reaction.PostId,
                UserId = reaction.UserId,
                RemoteIp = reaction.RemoteIp,
                Time = reaction.Time,
                IsLike = resultingReaction
            };

            var videoEvent = ReactingEvent.Create(eventData, eventData.EventId);
            _context.Add(videoEvent);

            var recommendationEventId = GuidService.GetNewGuid();
            var recommendationEvent = new UserInteractionRecordedV1
            {
                EventId = recommendationEventId,
                OccurredAt = DateTimeService.Now(),
                UserId = reaction.UserId,
                PostId = reaction.PostId,
                Type = resultingReaction switch
                {
                    true => UserInteractionType.Like,
                    false => UserInteractionType.Dislike,
                    null => UserInteractionType.ReactionRemoved
                },
                WatchedSeconds = reaction.Time,
                Reaction = resultingReaction,
                PreviousReaction = previousReaction
            };
            _context.Add(ReactingEvent.Create(recommendationEvent, recommendationEventId));
            await _context.SaveChangesAsync();
        }

        public async Task SetViewToPost(Domain.Events.VideoViewEvent videoView)
        {
            var videoEvent = ReactingEvent.Create(videoView, GuidService.GetNewGuid());
            _context.Add(videoEvent);
            await _context.SaveChangesAsync();
        }
    }
}
