using Blog.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using System.Text.Json;
using ViewReacting.Domain.Entities;
using ViewReacting.Domain.Events;
using ViewReacting.Domain.Services;

namespace VideoReacting.Service.Implementation
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
              .Where(x => (x.UserId == reaction.UserId || x.IpAddress == reaction.RemoteIp) && x.PostId == reaction.PostId)
              .FirstOrDefaultAsync();

            if (hasView == null)
            {
                hasView = new PostReaction(reaction.UserId, reaction.RemoteIp, reaction.PostId, reaction.Time, reaction.IsLike);
                _context.Add(hasView);
            }
            else
            {
                _context.Attach(hasView);
                hasView.IsLike = reaction.IsLike;
            }

            var eventData = new UserReactionSyncEvent
            {
                EventId = GuidService.GetNewGuid(),
                PostId = reaction.PostId,
                UserId = reaction.UserId,
                Time = reaction.Time,
                IsLike = reaction.IsLike
            };

            var videoEvent = ReactingEvent.Create(eventData, eventData.EventId);
            _context.Add(videoEvent);
            await _context.SaveChangesAsync();
        }

        public async Task SetViewToPost(VideoViewEvent videoView)
        {
            var videoEvent = ReactingEvent.Create(videoView, GuidService.GetNewGuid());
            _context.Add(videoEvent);
            await _context.SaveChangesAsync();
        }
    }
}