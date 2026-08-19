using Blog.Contracts.Events;
using Blog.Contracts.Models;
using Blog.Domain.Entities;
using Infrastructure.Services;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using System.Net;
using System.Text.Json;

namespace Blog.Service.EventHandlers;

public sealed class SyncProfileViewsHandler : IEventHandler<UserViewedSyncEvent>, IEventHandler<UserReactionSyncEvent>
{
    private readonly IReadWriteRepository<IBlogEntity> _context;
    private readonly ICacheService _cacheService;

    public SyncProfileViewsHandler(IReadWriteRepository<IBlogEntity> context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task Handle(IMessageContext<UserViewedSyncEvent> @event)
    {
        var userId = @event.Message.UserId;
        var ipAddress = @event.Message.RemoteIp;

        var post = await _context.Get<Post>()
            .Include(x=>x.VideoPostInfo)
            .FirstAsync(x => x.Id == @event.Message.PostId);

        var existView = await _context.Get<PostViewer>()
            .Where(x => x.PostId == post.Id)
            .Where(x => userId.HasValue
                ? x.UserId == userId
                : x.UserId == null && x.UserIpAddress == ipAddress)
            .FirstOrDefaultAsync();

        _context.Attach(post);

        if (existView == null)
        {
            if (@event.Message.IsViewed)
            {
                post.ViewCount++;
            }
            existView = new PostViewer
            {
                Id = GuidService.GetNewGuid(),
                PostId = @event.Message.PostId,
                UserId = userId,
                UserIpAddress = ipAddress,
                IsViewed = @event.Message.IsViewed,
            };
            _context.Add(existView);
        }
        else
        {
            if (@event.Message.IsViewed)
            {
                post.ViewCount++;
            }
            _context.Attach(existView);
            existView.UserId = userId;
            existView.UserIpAddress = ipAddress;
            existView.IsViewed = @event.Message.IsViewed;
        }
        await _cacheService.RemoveCachedDataAsync(new PostDetailViewModelCacheKey(post.Id));
        var postUpdateEvent = new PostUpdateEvent
        {
            BlogId = post.BlogId,
            PostId = post.Id,
            CreatedAt = post.CreatedAt,
            Description = post.VideoPostInfo.Description,
            Title = post.Title,
            UpdateType = UpdateType.Update,
            ViewCount = post.ViewCount,
        };
        _context.Add(VideoProcessEvent.Create(postUpdateEvent));
        await _context.SaveChangesAsync();
    }

    public async Task Handle(IMessageContext<UserReactionSyncEvent> @event)
    {
        var userId = @event.Message.UserId;
        var remoteIp = @event.Message.RemoteIp;

        var existView = await _context.Get<PostViewer>()
        .Where(x => x.PostId == @event.Message.PostId)
        .Where(x => userId.HasValue
            ? x.UserId == userId
            : x.UserId == null && x.UserIpAddress == remoteIp)
        .FirstOrDefaultAsync();

        var post = await _context.Get<Post>()
            .AsTracking()
            .FirstAsync(x => x.Id == @event.Message.PostId);
        if (existView == null)
        {
            if (@event.Message.IsLike == true)
            {
                post.LikeCount++;
            }
            if (@event.Message.IsLike == false)
            {
                post.DislikeCount++;
            }
            existView = new PostViewer
            {
                Id = GuidService.GetNewGuid(),
                PostId = @event.Message.PostId,
                IsLike = @event.Message.IsLike,
                UserId = userId,
                UserIpAddress = remoteIp
            };
            _context.Add(existView);
        }
        else
        {
            if (existView.IsLike != @event.Message.IsLike)
            {
                if (existView.IsLike == true)
                {
                    post.LikeCount = Math.Max(post.LikeCount - 1, 0);
                }
                else if (existView.IsLike == false)
                {
                    post.DislikeCount = Math.Max(post.DislikeCount - 1, 0);
                }

                if (@event.Message.IsLike == true)
                {
                    post.LikeCount++;
                }
                else if (@event.Message.IsLike == false)
                {
                    post.DislikeCount++;
                }
            }

            _context.Attach(existView);
            existView.IsLike = @event.Message.IsLike;
            existView.UserId = userId;
            existView.UserIpAddress = remoteIp;
        }
        await _context.SaveChangesAsync();
        await _cacheService.RemoveCachedDataAsync(new PostDetailViewModelCacheKey(post.Id));
    }

    //public async Task Handle(MessageContext @event)
    //{
    //    switch (@event.Message)
    //    {
    //        case UserReactionSyncEvent command:
    //            await Handle(MessageContext.Create(@event.CorrelationId, command));
    //            break;
    //        case UserViewedSyncEvent response:
    //            await Handle(MessageContext.Create(@event.CorrelationId, response));
    //            break;
    //        default:
    //            throw new ArgumentException();
    //    }
    //}
}
